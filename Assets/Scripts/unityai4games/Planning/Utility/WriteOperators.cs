using BoltFreezer.DecompTools;
using CompilationNamespace;
using PlanningNamespace;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
public class WriteOperators : MonoBehaviour {

    public List<GameObject> Operators;
    public bool do_writing = false;
    public string filename = "D:/documents/frostbow/cached/UnityDomain.txt";
	
	// Update is called once per frame
	void Update () {
		if (do_writing)
        {
            do_writing = false;
            
            DoWriting();
        }
	}

    public void DoWriting()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var op in Operators)
        {
            Debug.Log(op.name);
            try
            {
                // if op is domain operator
                var uao = op.GetComponent<UnityActionOperator>();
                sb.Append(WriteActionOp(op, uao));
            }
            catch
            {
                var utd = op.GetComponent<UnityTimelineDecomp>();
                sb.Append(WriteTimelineOp(op, utd));
            }
        }

        using (StreamWriter writer = new StreamWriter(filename, false))
        {
            writer.Write(sb.ToString());
        }
        Debug.Log("finished writing");
        sb = null;
    }

    public string WriteEveryItemFromList(string kingName, List<string> listOfNames)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(kingName + ": ");
        bool skip_line = false;
        var last_name = listOfNames.Last();
        foreach (var name in listOfNames)
        {
            
            sb.Append(name + "\t");
            if (skip_line)
            {
                if (last_name.Equals(name))
                {
                    break;
                }
                sb.Append("\n\t\t\t");
                skip_line = false;
            }
            else
            {
                skip_line = true;
            }
        }
        sb.Append("\n\t");
        return sb.ToString();
    }

    public string WriteActionOp(GameObject op, UnityActionOperator uao)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine(string.Format("action: {0}", op.name));
        sb.AppendLine("\ttype: primitive");
        sb.Append("\tterms: ");
        
        for (int i = 0; i < uao.MutableParameters.Count; i++)
        {
            sb.Append(i.ToString() + " - " + uao.MutableParameters[i].name + "\t");
            if (i%2 != 0 && i > 0)
            {
                if (i == uao.MutableParameters.Count)
                {
                    break;
                }
                sb.Append("\n\t\t\t");
            }
        }
        sb.AppendLine(WriteEveryItemFromList("\n\tpreconditions", uao.MutablePreconditions.Select(mp => mp.ToString()).ToList()));
        foreach(var inequality in uao.NonEqualityConstraints)
        {
            sb.Append(string.Format("\t(not (= {0} {1}))\t", inequality.first.ToString(), inequality.second.ToString()));
        }
        
        if (uao.MutableEffects.Count > 0)
        {
            sb.AppendLine(WriteEveryItemFromList("\n\teffects", uao.MutableEffects.Select(mp => mp.ToString()).ToList()));
        }
        
        sb.AppendLine(WriteEveryItemFromList("\tinstructions", uao.UnityInstructions.Select(mp => mp.ToString()).ToList()));
        sb.Append("\n\n");
        return sb.ToString();
    }

    public string WriteTimelineOp(GameObject op, UnityTimelineDecomp utd)
    {
        utd.reset = true;
        utd.Read();
        utd.Assemble();
        utd.NonGroundTimelineDecomposition();
        var partialDecomp = utd.PartialDecomp;
        var csc = new CompositeScheduleComposer(utd, partialDecomp);
        var comp = csc.CreateCompositeSchedule();
        var numCamSteps = comp.NumberCamSteps;
        comp.Height= Mathf.Max(numCamSteps, comp.SubSteps.Count - numCamSteps);

        StringBuilder sb = new StringBuilder();

        sb.AppendLine(string.Format("decomposition: {0}", op.name));
        sb.AppendLine("\ttype: composite");
        sb.AppendLine(string.Format("\tvalue: {0}", comp.Height));

        sb.Append("\tterms: ");

        for (int i = 0; i < partialDecomp.Terms.Count; i++)
        {
            var termI = partialDecomp.Terms[i];
            sb.Append(termI.ToString() + " - " + termI.Type + "\t");
            if (i % 2 != 0 && i > 0)
            {
                if (i == partialDecomp.Terms.Count)
                {
                    break;
                }
                sb.Append("\n\t\t\t");
            }
        }


        if (comp.Preconditions.Count > 0)
        {
            sb.Append(WriteEveryItemFromList("preconditions", comp.Preconditions.Select(mp => mp.ToString()).ToList()));
        }
        foreach (var inequality in partialDecomp.NonEqualities)
        {
            sb.Append(string.Format("(not (= {0} {1}))\t", inequality[0].ToString(), inequality[1].ToString()));
        }
        if (comp.Effects.Count > 0)
        {
            sb.AppendLine(WriteEveryItemFromList("\n\teffects", comp.Effects.Select(mp => mp.ToString()).ToList()));
        }

        if (partialDecomp.SubSteps.Count > 0)
        {
            sb.AppendLine(WriteEveryItemFromList("\n\tsub-steps", partialDecomp.SubSteps.Select(mp => mp.ToString()).ToList()));
        }
        if (partialDecomp.SubOrderings.Count > 0)
        {
            sb.AppendLine(WriteEveryItemFromList("\n\tsub-orderings", partialDecomp.SubOrderings.Select(mp => mp.ToString()).ToList()));
        }
        if (partialDecomp.SubLinks.Count > 0)
        {
            sb.AppendLine(WriteEveryItemFromList("\n\tsub-links", partialDecomp.SubLinks.Select(mp => mp.ToString()).ToList()));
        }
        if (partialDecomp.fabCntgs.Count > 0)
        {
            sb.AppendLine(WriteEveryItemFromList("\n\tsub-cntgs", partialDecomp.fabCntgs.Select(mp => mp.ToString()).ToList()));
        }
        if (partialDecomp.fabConstraints.Count > 0)
        {
            sb.AppendLine(WriteEveryItemFromList("\n\tfab-constraints", partialDecomp.fabConstraints.Select(mp => mp.ToString()).ToList()));
        }
        if (partialDecomp.discourseSubSteps.Count > 0)
        {
            sb.AppendLine("\n\tshots: \n");
            foreach (var camstep in partialDecomp.discourseSubSteps)
            {
                //var camdetails = camstep.CamDetails;
                var targetdetails = camstep.TargetDetails;
                var asof = targetdetails.actionSegOfFocus;
                var ot = targetdetails.orientTowards;
                sb.AppendLine("\t\t " + camstep.ToString());
                sb.AppendLine("\t\t" + camstep.CamDetails.ToString());
                sb.AppendLine("\t\tInterval-Of-Focus: " + asof.ToString());
                sb.AppendLine("\t\tActionIntervals:\n");
                foreach(var aseg in targetdetails.ActionSegs)
                {
                    sb.AppendLine(string.Format("\t\t\t actionVarName: {0} [{1}%: {2}%], id={3}, type={4}, target: {5}", aseg.actionVarName, aseg.startPercent, aseg.endPercent, aseg.ActionID, aseg.actiontypeID, aseg.targetVarName));
                    sb.AppendLine(string.Format("\t\t\t composition: {0}, directive: {1}", aseg.screenxy, aseg.directive));
                }

                sb.AppendLine("\n");

            }
        }
        if (partialDecomp.discOrderings.Count > 0)
        {
            sb.AppendLine(WriteEveryItemFromList("\n\tshot-orderings", partialDecomp.discOrderings.Select(mp => mp.ToString()).ToList()));
        }
        if (partialDecomp.discLinks.Count > 0)
        {
            sb.AppendLine(WriteEveryItemFromList("\n\tshot-links", partialDecomp.discLinks.Select(mp => mp.ToString()).ToList()));
        }
        if (partialDecomp.discCntgs.Count > 0)
        {
            sb.AppendLine(WriteEveryItemFromList("\n\tshot-cntgs", partialDecomp.discCntgs.Select(mp => mp.ToString()).ToList()));
        }
        if (partialDecomp.discConstraints.Count > 0)
        {
            sb.AppendLine(WriteEveryItemFromList("\n\tshot-constraints", partialDecomp.discConstraints.Select(mp => mp.ToString()).ToList()));
        }


        sb.Append("\n\n");
        return sb.ToString();

    }
}
