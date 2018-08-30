using BoltFreezer.CacheTools;
using BoltFreezer.DecompTools;
using BoltFreezer.FileIO;
using BoltFreezer.Interfaces;
using BoltFreezer.PlanTools;
using BoltFreezer.Scheduling;
using CompilationNamespace;
using PlanningNamespace;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class RaceBlockProblemGenerator : MonoBehaviour {

    public CacheManager cacheManager;
    public bool trigger_generation = false;
    public int num_problems = 20;
	
	// Update is called once per frame
	void Update () {
		if (trigger_generation)
        {
            trigger_generation = false;
            GenerateProblems();
        }
	}

    public void GenerateProblems()
    {
        Parser.path = "/";

        
        DiscourseDecompositionHelper.SetCamsAndLocations(GameObject.FindGameObjectWithTag("CameraHost"), GameObject.FindGameObjectWithTag("Locations"));
        var domainOperatorComponent = GameObject.FindGameObjectWithTag("ActionHost").GetComponent<DomainOperators>();
        domainOperatorComponent.Reset();
        var domain = CreateDomain(domainOperatorComponent);
        var initPlan = PrepareProblem(domainOperatorComponent, domain, 0);


        var UGAF = GameObject.Find("GroundActionFactory").GetComponent<UnityGroundActionFactory>();
        var DecompSchemata = UGAF.DecompositionSchemata;
        

        
        // Compile the Decomposition Schedule Schemata
        foreach (var unitydecomp in DecompSchemata)
        {
            unitydecomp.GroundDecomps = new List<TimelineDecomposition>();
            unitydecomp.reset = true;
            unitydecomp.Read();
            unitydecomp.Assemble();
            unitydecomp.NonGroundTimelineDecomposition();
            //unitydecomp.Filter();
            Debug.Log("Read and Assemble for unity decomp: " + unitydecomp.name);
        }


        string originalPlanName = cacheManager.problemname;
        for (int i = 0; i < num_problems; i++)
        {
            initPlan = PrepareProblem(domainOperatorComponent, domain, i);
            GroundActionFactory.GroundActions = new HashSet<IOperator>(GroundActionFactory.GroundActions).ToList();
            // AddObservedNegativeConditions(UPC);
            CreateSteps(DecompSchemata, 1, initPlan.InitialStep.Effects, initPlan.GoalStep.Preconditions);
            cacheManager.CacheIt(originalPlanName, originalPlanName + i.ToString() + "_");
        }


    }

    public IPlan PrepareProblem(DomainOperators domainOperatorComponent, Domain domain, int i)
    {
        
        var problem = CreateProblem(domainOperatorComponent.DomainOps, i);
        Directory.CreateDirectory(@"D:\Documents\Frostbow\Benchmarks\Problems\");
        WriteProblemToFile(problem,  @"D:\Documents\Frostbow\Benchmarks\Problems\");
        // Create Problem Freezer.
        var PF = new ProblemFreezer("Unity", "", domain, problem);
        // Create Initial Plan
        var initPlan = PlannerScheduler.CreateInitialPlan(PF);
        initPlan = PreparePOCL(initPlan, domain, problem, PF);
        return initPlan;
    }

    public IPlan PreparePOCL(IPlan initPlanSchema, Domain domain, Problem problem, ProblemFreezer PF)
    {
        // Reset Cache
        GroundActionFactory.Reset();
        CacheMaps.Reset();

        GroundActionFactory.PopulateGroundActions(domain, problem);

        // Remove Irrelevant Actions (those which require an adjacent edge but which does not exist. In Refactoring--> make any static
        Debug.Log("removing irrelevant actions");
        var adjInitial = initPlanSchema.Initial.Predicates.Where(state => IsStatic(state.Name));
        var replacedActions = new List<IOperator>();
        foreach (var ga in GroundActionFactory.GroundActions)
        {
            // If this action has a precondition with name adjacent this is not in initial state, then it's impossible. True ==> impossible. False ==> OK!
            var isImpossible = ga.Preconditions.Where(pre => IsStatic(pre.Name) && pre.Sign).Any(pre => !adjInitial.Contains(pre));
            if (isImpossible)
                continue;
            replacedActions.Add(ga);
        }
        GroundActionFactory.Reset();
        GroundActionFactory.GroundActions = replacedActions;
        GroundActionFactory.GroundLibrary = replacedActions.ToDictionary(item => item.ID, item => item);

        // Detect Statics
        Debug.Log("Detecting Statics");
        GroundActionFactory.DetectStatics();
        RemoveStaticPreconditions(GroundActionFactory.GroundActions);

        // Cache links, now not bothering with statics
        CacheMaps.CacheLinks(GroundActionFactory.GroundActions);
        CacheMaps.CacheGoalLinks(GroundActionFactory.GroundActions, problem.Goal);


        Debug.Log("Caching Heuristic costs");
        CacheMaps.CacheAddReuseHeuristic(initPlanSchema.Initial);

        // Recreate Initial Plan
        return PlannerScheduler.CreateInitialPlan(PF);
    }

    public Problem CreateProblem(List<Operator> DomainOps, int k)
    {
        // Here, create replica of problem components
        /*
         * how many boids? between 1 and 3
         * how many locations between #boids + 1 and #boids + 4
         * how many blocks? between 1 and max(#locations-#boids, 3)
         * how many placeable locations? between #blocks+1 and max(#locations, #blocks+3)
         */
        var objs = new List<IObject>();

        int numBoids = Random.Range(1, 4);
        var boids = new List<ITerm>();
        var boidNames = new List<string>() { "orange", "blue", "red" };
        var freehands = new List<IPredicate>();
        for (int i = 0; i < numBoids; i++)
        {
            var nextBoid = new Term(boidNames[i], true) as ITerm;
            boids.Add(nextBoid);
            objs.Add(new Obj(boidNames[i], "SteeringAgent") as IObject);
            var nextFreeHands = new Predicate("freehands", new List<ITerm>() { nextBoid }, true) as IPredicate;
            freehands.Add(nextFreeHands);
        }
        

        int numLocs = Random.Range(numBoids + 1, Mathf.Min(numBoids + 3, 6));
        var locs = new List<ITerm>();
        var locNames = new List<string>() { "L0", "L1", "L2", "L3", "L4" };
        for (int i = 0; i < numLocs; i++)
        {
            var nextLoc = new Term(locNames[i], true) as ITerm;
            locs.Add(nextLoc);
            objs.Add(new Obj(locNames[i], "Walkable") as IObject);
        }
        var adjacents = DecideAdjacentLocs(locs);

        int numBlocks = Random.Range(1, Mathf.Min(4, numLocs));
        var blocks = new List<ITerm>();
        var blockNames = new List<string>() { "blockA", "blockB", "blockC" };
        for (int i = 0; i < numBlocks; i++)
        {
            var nextBlock = new Term(blockNames[i], true) as ITerm;
            blocks.Add(nextBlock);
            objs.Add(new Obj(blockNames[i], "Block") as IObject);
        }

        int numPlaces = Random.Range(numBlocks+1, Mathf.Min(numLocs, numBlocks + 3));
        var places = new List<ITerm>();
        var placeNames = new List<string>() { "L0A", "L1A", "L2A", "L3A", "L4A" };
        var placeables = new List<IPredicate>();
        for (int i = 0; i < numPlaces; i++)
        {
            var nextPlace = new Term(placeNames[i], true) as ITerm;
            places.Add(nextPlace);
            var nextPlacelableLit = new Predicate("placeable", new List<ITerm>() { nextPlace, locs[i] }, true) as IPredicate;
            placeables.Add(nextPlacelableLit);
            objs.Add(new Obj(placeNames[i], "Aux") as IObject);
        }

        // put blocks at locations
        var locSet = new HashSet<ITerm>(places);
        var blockSet = new HashSet<ITerm>(blocks);
        var blockLocs = new List<IPredicate>();
        while (blockSet.Count > 0)
        {
            var block = blockSet.FirstOrDefault();
            blockSet.Remove(block);
            var loc = locSet.FirstOrDefault();
            locSet.Remove(loc);
            var newLit = new Predicate("at", new List<ITerm>() { block, loc }, true) as IPredicate;
            blockLocs.Add(newLit);
            newLit = new Predicate("occupied", new List<ITerm>() { loc }, true) as IPredicate;
            blockLocs.Add(newLit);
        }

        // put boids at locations
        locSet = new HashSet<ITerm>(locs);
        var boidSet = new HashSet<ITerm>(boids);
        var boidLocs = new List<IPredicate>();
        while (boidSet.Count > 0)
        {
            var boid = boidSet.FirstOrDefault();
            boidSet.Remove(boid);
            var loc = locSet.FirstOrDefault();
            locSet.Remove(loc);
            var newLit = new Predicate("at", new List<ITerm>() { boid, loc }, true) as IPredicate;
            boidLocs.Add(newLit);
            newLit = new Predicate("occupied", new List<ITerm>() { loc }, true) as IPredicate;
            boidLocs.Add(newLit);
        }

        // Assemble initial state
        var initPredicateList = new List<IPredicate>();
        initPredicateList.AddRange(blockLocs);
        initPredicateList.AddRange(placeables);
        initPredicateList.AddRange(adjacents);
        initPredicateList.AddRange(boidLocs);
        initPredicateList.AddRange(freehands);
        

        // pick a block and a location that the block isn't at.
        bool found = false;
        locSet = new HashSet<ITerm>(places);
        blockSet = new HashSet<ITerm>(blocks);
        var block_goal = blockSet.FirstOrDefault();

        var goalPredicateList = new List<IPredicate>();
        while (!found)
        {
            var loc = locSet.FirstOrDefault();
            locSet.Remove(loc);

            var occ = new Predicate("occupied", new List<ITerm>() { loc }, true) as IPredicate;

            var newLit = new Predicate("at", new List<ITerm>() { block_goal, loc }, true) as IPredicate;
            if (!initPredicateList.Contains(occ))
            {
                found = true;
                goalPredicateList.Add(newLit);
            }
            if (!found)
            {
                if (locSet.Count == 0)
                {
                    Debug.Log("mistake");
                    throw new System.Exception();
                }
            }
        }


       

        //// Calculate Objects
        //var objects = new List<IObject>();
        //foreach (var location in locations)
        //{
        //    objects.Add(new Obj(location.name, location.tag) as IObject);
        //}
        //foreach (var actor in actors)
        //{
        //    var superordinateTypes = GetSuperOrdinateTypes(actor.tag);
        //    objects.Add(new Obj(actor.name, actor.tag) as IObject);
        //}


        var probName = cacheManager.problemname + k.ToString() + "_";
        var prob = new Problem(probName, probName, "Unity", "", objs, initPredicateList, goalPredicateList);
        return prob;
    }

    public List<IPredicate> DecideAdjacentLocs(List<ITerm> locs)
    {
        var adjDict = new Dictionary<int, bool>();
        var adjacents = new List<IPredicate>();
        for (int i = 0;  i < locs.Count; i++)
        {
            if (!adjDict.ContainsKey(i))
            {
                adjDict[i] = false;
            }
            for (int j = 0; j < locs.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                
                if (adjDict[i] == true)
                {
                    if (Random.Range(0,2) == 0)
                    {
                        continue;
                    }
                }
                else
                {
                    if (adjacents.Count > 0)
                    {
                        if (adjDict[j] == false)
                        {
                            continue;
                        }
                    }
                }

                adjDict[i] = true;
                if (!adjDict.ContainsKey(j))
                {
                    adjDict[j] = true;
                }

                var newLit = new Predicate("adjacent", new List<ITerm>() { locs[i], locs[j] }, true);
                var newLit2 = new Predicate("adjacent", new List<ITerm>() { locs[j], locs[i] }, true);
                if (adjacents.Contains(newLit))
                {
                    continue;
                }

                adjacents.Add(newLit);
                adjacents.Add(newLit2);

            }
        }
        
        return adjacents;
    }

    public Domain CreateDomain(DomainOperators domainOperatorComponent)
    {
        var newOps = new List<IOperator>();

        foreach (var domainOp in domainOperatorComponent.DomainOps)
        {
            newOps.Add(domainOp as IOperator);
        }
        var domain = new Domain("unityWorld", BoltFreezer.Enums.PlanType.PlanSpace, newOps);
        domain.AddTypePair("SteeringAgent", "Agent");
        domain.AddTypePair("Block", "Item");
        domain.AddTypePair("Walkable", "Location");
        domain.AddTypePair("Aux", "Location");
        domain.AddTypePair("", "SteeringAgent");
        domain.AddTypePair("", "Block");
        domain.AddTypePair("", "Walkable");
        domain.AddTypePair("", "Aux");


        return domain;
    }

    public List<IObject> GetObjects()
    {
        var locationHost = GameObject.FindGameObjectWithTag("Locations");
        var locations = Enumerable.Range(0, locationHost.transform.childCount).Select(i => locationHost.transform.GetChild(i)).Where(item => item.gameObject.activeSelf);
        var actorHost = GameObject.FindGameObjectWithTag("ActorHost");
        var actors = Enumerable.Range(0, actorHost.transform.childCount).Select(i => actorHost.transform.GetChild(i));

        // Calculate Objects
        var objects = new List<IObject>();
        foreach (var location in locations)
        {
            objects.Add(new Obj(location.name, location.tag) as IObject);
        }
        foreach (var actor in actors)
        {
            var superordinateTypes = GetSuperOrdinateTypes(actor.tag);
            objects.Add(new Obj(actor.name, actor.tag) as IObject);
        }
        return objects;
    }

    public List<string> GetSuperOrdinateTypes(string subtype)
    {
        var parentGo = GameObject.Find(subtype).transform;

        var superOrdinateTypes = new List<string>();
        superOrdinateTypes.Add(subtype);
        while (true)
        {
            parentGo = parentGo.parent;
            var parentName = parentGo.name;
            if (parentName.Equals("TypeHierarchy"))
            {
                break;
            }
            superOrdinateTypes.Add(parentName);
        }

        return superOrdinateTypes;
    }

    public static void RemoveStaticPreconditions(List<IOperator> groundActions)
    {
        foreach (var ga in groundActions)
        {
            List<IPredicate> newPreconds = new List<IPredicate>();
            foreach (var precon in ga.Preconditions)
            {
                if (GroundActionFactory.Statics.Contains(precon))
                {
                    continue;
                }
                //if (IsPrimaryEffect(precon))
                //{
                //    var termAsPred = precon.Terms[0] as IPredicate;
                //    if (termAsPred != null)
                //    {
                //        if (GroundActionFactory.Statics.Contains(termAsPred))
                //        {
                //            continue;
                //        }
                //    }
                //}
                newPreconds.Add(precon);
            }

            ga.Preconditions = newPreconds;
        }
    }

    public static bool IsStatic(string s)
    {
        if (s.Equals("adjacent"))
        {
            return true;
        }
        if (s.Equals("placeable"))
        {
            return true;
        }
        return false;
    }

    public void CreateSteps(List<UnityTimelineDecomp> DecompositionSchemata, int heightMax, List<IPredicate> initList, List<IPredicate> goalList)
    {

        // For each height
        for (int h = 0; h < heightMax; h++)
        {
            var newopsThisRound = new List<IOperator>();
            foreach (var utd in DecompositionSchemata)
            {
                var td = utd.PartialDecomp;
                var gdecomps = TimelineDecompositionHelper.Compose(h, td);
                foreach (var gdecomp in gdecomps)
                {
                    var csc = new CompositeScheduleComposer(utd, gdecomp);
                    var comp = csc.CreateCompositeSchedule();
                    if (comp.Effects.Count == 0)
                    {
                        Debug.Log("couldn't create " + comp.ToString());
                        continue;
                    }
                    //comp.Height = h+1;
                    comp.Height = UnityGroundActionFactory.GetModifiedHeightValue(comp);
                    newopsThisRound.Add(comp as IOperator);
                }
            }

            foreach (var op in newopsThisRound)
            {
                GroundActionFactory.InsertOperator(op);
            }
            if (newopsThisRound.Count == 0)
            {
                break;
            }
        }

        CacheMaps.Reset();
        CacheMaps.CacheLinks(GroundActionFactory.GroundActions);
        CacheMaps.CacheGoalLinks(GroundActionFactory.GroundActions, goalList);

        CacheMaps.PrimaryEffectHack(new State(initList) as IState);
    }

    public static void WriteProblemToFile(Problem problem, string directory)
    {
        var file = directory + problem.Name + "problem.txt";

        using (StreamWriter writer = new StreamWriter(file, false))
        {
            writer.Write(problem.ToString());
        }
    }

    public static IPredicate ParenthesisStringToPredicate(string stringItem)
    {
        var splitInput = stringItem.Split(' ');
        var predName = splitInput[0].TrimStart('(');
        var terms = new List<ITerm>();
        foreach (string item in splitInput.Skip(1))
        {
            var cleanedItem = item.TrimEnd(')');
            var newTerm = new Term(cleanedItem, true) as ITerm;
            terms.Add(newTerm);
        }
        var predic = new Predicate(predName, terms, true) as IPredicate;
        return predic;
    }

    public static Problem ReadStringGeneratedProblem(string file, int problemNumber)
    {
        string[] input = System.IO.File.ReadAllLines(file);

        List<IObject> problemObjects = new List<IObject>();
        List<IPredicate> initialPreds = new List<IPredicate>();
        List<IPredicate> goalPreds = new List<IPredicate>();

        // objects, then initial state, then goal state
        int i = 3;
        bool onObjects = true;
        bool onInit = false;
        while (true)
        {
            if (onObjects)
            {

                var objType = input[i].Split('_').First();
                if (objType.Equals("agent"))
                {
                    objType = "steeringagent";
                }
                var newObject = new Obj(input[i], objType) as IObject;
                problemObjects.Add(newObject);
                i++;
                if (input[i].Equals("") || input[i].Equals("\n"))
                {
                    onObjects = false;
                    onInit = true;
                    i = i + 2;
                }
            }

            if (onInit)
            {

                var newInit = ParenthesisStringToPredicate(input[i]);
                initialPreds.Add(newInit);
                i++;

                if (input[i].Equals("") || input[i].Equals("\n"))
                {
                    onInit = false;
                    i = i + 2;
                }
            }

            if (!onInit && !onObjects)
            {
                var pred = ParenthesisStringToPredicate(input[i]);
                goalPreds.Add(pred);
                i++;
            }

            if (i >= input.Count())
            {
                break;
            }

        }

        // create new Problem
        var prob = new Problem(problemNumber.ToString(), problemNumber.ToString(), "raceblocks", "", problemObjects, initialPreds, goalPreds);
        return prob;
    }
}
