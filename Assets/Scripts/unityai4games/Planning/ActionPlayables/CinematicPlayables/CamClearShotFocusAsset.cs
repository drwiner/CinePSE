using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


namespace TimelineClipsNamespace
{
    public class CamClearShotFocusAsset : PlayableAsset
    {
        public ExposedReference<CinemachineClearShot> CCS;
        public ExposedReference<Transform> Target;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CamClearShotFocusPlayable>.Create(graph);
            var camFocusPlayable = playable.GetBehaviour();

            var ccb= CCS.Resolve(playable.GetGraph().GetResolver());
            var transf = Target.Resolve(playable.GetGraph().GetResolver());

            camFocusPlayable.Initialize(ccb, transf);
            return playable;
        }
    }

}