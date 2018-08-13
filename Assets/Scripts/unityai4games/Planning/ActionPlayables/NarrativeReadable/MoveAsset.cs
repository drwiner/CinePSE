using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BoltFreezer.Camera.CameraEnums;
using PlanningNamespace;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


namespace TimelineClipsNamespace
{
    [Serializable]
    public class MoveAsset : PlayableAsset
    {
        [SerializeField]
        public ExposedReference<UnityActionOperator> MoveOperator;

        [SerializeField]
        public ExposedReference<GameObject> Agent;

        [SerializeField]
        public ExposedReference<GameObject> Origin;

        [SerializeField]
        public ExposedReference<GameObject> Destination;

        public UnityActionOperator moveSchema;
        public GameObject agent;
        public GameObject origin;
        public GameObject destination;

        //protected ScriptPlayable<FabulaPlayable> playableFabula;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {

            var playable = ScriptPlayable<BlankPlayable>.Create(graph);

            agent = Agent.Resolve(playable.GetGraph().GetResolver());
            origin = Origin.Resolve(playable.GetGraph().GetResolver());
            destination = Destination.Resolve(playable.GetGraph().GetResolver());

            //fabPlayable.Initialize(Agent, Origin, Destination);

            return playable;
        }

    }

    // This blank playable allows the asset to be read-only
    [Serializable]
    public class BlankPlayable : PlayableBehaviour
    {

    }


}