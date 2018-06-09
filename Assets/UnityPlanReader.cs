using BoltFreezer.Camera;
using BoltFreezer.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace PlanningNamespace
{
    [ExecuteInEditMode]
    public class UnityPlanReader : MonoBehaviour
    {
        public UnityPlanningInterface PlanDirector;

       // public GameObject FabulaTimelineHost;
        //private PlayableDirector fabulaDirector;
        public List<string> fabulaStepNames = new List<string>();
        public List<IPlanStep> fabulaSteps = new List<IPlanStep>();

       // public GameObject DiscoureTimelineHost;
       // private PlayableDirector discourseDirector;
        public List<string> discourseStepNames = new List<string>();
        public List<IPlanStep> discourseSteps = new List<IPlanStep>();

        public bool assembleClips = false;

        // Update is called once per frame
        void Update()
        {
            if (assembleClips)
            {
                assembleClips = false;

                AssembleClips();
            }
        }

        public void AssembleClips()
        {
            // Execution Timelines
            //fabulaDirector = FabulaTimelineHost.GetComponent<PlayableDirector>();
            //discourseDirector = DiscoureTimelineHost.GetComponent<PlayableDirector>();

            var opNames = new List<string>();
            var domainOps = GameObject.FindGameObjectWithTag("ActionHost");
            for(int i = 0; i < domainOps.transform.childCount; i++)
            {
                opNames.Add(domainOps.transform.GetChild(i).name);
            }

            discourseStepNames = new List<string>();
            discourseSteps = new List<IPlanStep>();

            fabulaStepNames = new List<string>();
            fabulaSteps = new List<IPlanStep>();


            //double fabTimeCounter = 0;
            //double discTimeCounter = 0;
            foreach (var step in PlanDirector.Plan)
            {
                if (step.Height > 0)
                {
                    continue;
                }
                CamPlanStep cps = step as CamPlanStep;
                if (cps == null)
                {
                    if (!opNames.Contains(step.Name))
                    {
                        // then it's an initial or dummy
                        continue;
                    }
                    // then it's fabula
                    fabulaSteps.Add(step);
                    fabulaStepNames.Add(step.ToString());
                }
                else
                {
                    discourseSteps.Add(step);
                    discourseStepNames.Add(step.ToString());
                }
            }
        }
    }
}