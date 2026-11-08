using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        string spectateId,shownViewpoint;
        bool cameraCut;
        public bool IsSpectating=>View?.phase!=Phase.Lobby&&Self?.observer==true&&View.players.Any(p=>p.alive);
        public string ViewpointId=>externalCamera?"":IsSpectating?(View.players.FirstOrDefault(p=>p.id==spectateId&&p.alive)??View.players.First(p=>p.alive)).id:LocalId;
        public PlayerView CameraPlayer=>View?.players?.FirstOrDefault(p=>p.id==ViewpointId);
        public void CycleSpectator(int direction)
        {
            if(!IsSpectating)return;
            var living=View.players.Where(p=>p.alive).ToArray();
            int index=System.Array.FindIndex(living,p=>p.id==ViewpointId);
            spectateId=living[(index+direction+living.Length)%living.Length].id;cameraCut=true;
            RefreshViewpoint();
        }
        void UpdateSpectator()
        {
            if(IsSpectating)
            {
                spectateId=ViewpointId;
                if(!MenuOpen&&Input.GetKeyDown(KeyCode.LeftArrow))CycleSpectator(-1);
                if(!MenuOpen&&Input.GetKeyDown(KeyCode.RightArrow))CycleSpectator(1);
            }
            RefreshViewpoint();
        }
        void RefreshViewpoint()
        {
            string id=ViewpointId;if(id==shownViewpoint)return;
            shownViewpoint=id;cameraCut=true;cameraVelocity=Vector3.zero;
            foreach(var avatar in avatars)avatar.Value.SetViewpoint(avatar.Key==id);
        }

        bool UpdateDeathCamera()
        {
            if(Self?.alive!=false||Self.observer||!avatars.TryGetValue(LocalId,out var avatar)||avatar.DeathBody==null)return false;
            Vector3 look=avatar.DeathBody.Center+Vector3.up*.25f;
            Vector3 direction=Vector3.ProjectOnPlane(new Vector3(Self.deathX,Self.deathY,Self.deathZ),Vector3.up).normalized;
            if(direction.sqrMagnitude<.1f)direction=Quaternion.Euler(0,Self.yaw,0)*Vector3.forward;
            Vector3 offset=-direction*2.6f+Vector3.Cross(direction,Vector3.up)*.8f+Vector3.up*1.3f;
            Vector3 target=look+offset;
            if(Physics.SphereCast(look,.13f,offset.normalized,out var hit,offset.magnitude,~((1<<8)|(1<<SaloonRagdoll.Layer)),QueryTriggerInteraction.Ignore))
                target=look+offset.normalized*Mathf.Max(.3f,hit.distance-.16f);
            if(cameraCut)ViewCamera.transform.position=target;
            else ViewCamera.transform.position=Vector3.SmoothDamp(ViewCamera.transform.position,target,ref cameraVelocity,.1f);
            ViewCamera.transform.rotation=Quaternion.LookRotation(look-ViewCamera.transform.position);
            ViewCamera.fieldOfView=Mathf.Lerp(ViewCamera.fieldOfView,78,Time.deltaTime*12);cameraCut=false;return true;
        }
    }
}
