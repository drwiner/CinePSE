using PlanningNamespace;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace TimelineClipsNamespace
{

    public class TimeTravelPlayable : PlayableBehaviour
    {
        private PlayableDirector _pd;
        private float _newTime;
        private float _endTime = -1f;
        private UnityProblemCompiler UPC;

        public void Initialize(PlayableDirector pd, float newtime)
        {
            _pd = pd;
            _newTime = newtime;
            if (_endTime < 0)
            {
                var ta = _pd.playableAsset as TimelineAsset;
                // make sure to get the last end time.
                foreach (var track in ta.GetOutputTracks())
                {
                    if (track.end > _endTime)
                    {
                        _endTime = (float)track.end;
                    }
                }
            }
            UPC = GameObject.FindGameObjectWithTag("Problem").GetComponent<UnityProblemCompiler>();
        }

        //public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        //{
        //    if (playable.GetTime() <= 0 || _pd == null)
        //        return;
        //}

        public void Rewind(float ft)
        {
            while (ft < _pd.time)
            {
                _pd.time -= .1f;
                _pd.Evaluate();
            }
        }

        public void FastForward(float ft)
        {
            while (ft > _pd.time)
            {
                _pd.time += .06f;
                _pd.Evaluate();
            }
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (Mathf.Abs((float)_newTime - (float)_pd.time) < 0.03)
            {
                return;
            }

            _pd.Pause();

            

            if (_newTime > _pd.time)
            {
                FastForward(_newTime);
            }
            else 
            {
                
                if (_newTime == 0)
                {
                    //var UPC = GameObject.FindGameObjectWithTag("Problem").GetComponent<UnityProblemCompiler>();
                    //Rewind(0);
                    _pd.time = 0f;
                    UPC.SetInitialState();
                }
                else
                {
                    //FastForward(_endTime);
                    
                    //Rewind(0);
                    _pd.time = 0f;
                    UPC.SetInitialState();
                    FastForward(_newTime);
                    //Rewind(_newTime + .06f);
                }
                // UPC.SetInitialState();
                //Rewind(_newTime - 0.06f);
                // FastForward(_newTime);
            }

            Debug.Log("setFabTime: " + _pd.time + " and: " + _pd.state);

            if (_pd.state != PlayState.Playing)
            {
                _pd.RebuildGraph();
                _pd.Play();
            }
        }
        //public override void OnBehaviourPause(Playable playable, FrameData info)
        //{
        //    if (info.evaluationType == FrameData.EvaluationType.Playback)
        //    {
        //    }
        //}
    }

}