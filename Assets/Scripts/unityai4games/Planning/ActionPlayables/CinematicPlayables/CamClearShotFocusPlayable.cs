using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace TimelineClipsNamespace
{

    public class CamClearShotFocusPlayable : PlayableBehaviour
    {
        private CinemachineClearShot _ccs;
        private Transform _target;
        private Transform _oldTarget;

        public void Initialize(CinemachineClearShot ccs, Transform target)
        {
            _ccs = ccs;
            _target = target;
        }


        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _oldTarget = _ccs.m_LookAt;
            _ccs.m_LookAt = _target;

        }
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            _ccs.m_LookAt = _oldTarget;
        }
    }

}