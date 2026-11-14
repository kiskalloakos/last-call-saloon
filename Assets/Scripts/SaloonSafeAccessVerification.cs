#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifySafeAccess()
        {
            verifying=true;AutomatedInput=true;VerificationPassed=VerificationFinished=false;
            int checks=0;
            void Check(bool ok,string message)
            {checks++;if(!ok){VerificationFinished=true;Debug.LogError("SALOON_SAFE_ACCESS FAIL "+message);throw new InvalidOperationException(message);}}
            var host=players[Connection.LocalId];var bot=players.Values.First(p=>p.bot);
            void Window(Role role)
            {
                ResetHeist();outcome="";roomTurns.Clear();duels.Clear();
                int i=0;foreach(var p in players.Values)
                {
                    p.hp=100;p.role=SaloonRules.Deck[i++];p.deathAt=-1;p.chosenRoom=p.room=0;p.companion=null;
                    p.busyUntil=0;p.busyAction="";p.lastAction=-100;p.beer=0;p.beerChoiceUntil=0;
                    p.botThink=Time.time+100;p.move=Vector2.zero;p.botMoveWorld=Vector3.zero;
                }
                host.role=role;bot.role=Role.Outlaw;
                rounds.Start(players.Keys,Time.time,300,300,0);foreach(var id in players.Keys)rounds.SetReady(id,true);
                rounds.Tick(Time.time,id=>true);phase=Phase.Turns;PositionEveryone(false);
            }
            void Place(Participant p,Vector3 position)
            {p.controller.enabled=false;p.seat=-1;p.body.transform.position=position;p.move=Vector2.zero;UpdatePlayerColliders(p);Physics.SyncTransforms();}
            void Cmd(string kind){host.lastAction=-100;OnCommand(host.id,new Command{kind=kind,x=SaloonArt.SafePosition.x,z=SaloonArt.SafePosition.z});}
            foreach(var role in new[]{Role.Outlaw,Role.Sheriff})
            {
                Window(role);string command=role==Role.Outlaw?"rob":"inspect";
                Cmd(command);
                Check(!rounds.HasSpent(host.id)&&host.busyAction==""&&host.safeReport==""&&safeProgress==0,role+" cannot use safe from the Bar stool or a spoofed command position");
                Place(host,SafeApproach+Vector3.up*3);Cmd(command);
                Check(!rounds.HasSpent(host.id),role+" cannot reach the safe from another height");
                Place(host,SafeApproach);host.room=1;Cmd(command);
                Check(!rounds.HasSpent(host.id),role+" cannot use safe while assigned to another room");host.room=0;
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position=SaloonArt.SafePosition+new Vector3(0,1,-.65f);wall.transform.localScale=new Vector3(1.5f,2,.08f);Physics.SyncTransforms();
                Cmd(command);Check(!rounds.HasSpent(host.id),role+" cannot interact through an obstruction");
                Destroy(wall);yield return null;Physics.SyncTransforms();
                Check(AtSafe(host),role+" can reach the actual safe through its clear public approach");
                Cmd(command);Check(rounds.HasSpent(host.id)&&(role==Role.Outlaw?host.busyAction=="rob":host.safeReport.Contains("money inside")),role+" can use safe beside it and spends exactly one action");
            }
            Window(Role.Outlaw);Place(host,SafeApproach);Cmd("rob");
            Place(host,new Vector3(-4,.08f,0));TickSafeWork(host);
            Check(host.busyAction==""&&host.busyUntil==0&&safeProgress==0&&rounds.HasSpent(host.id),"walking away interrupts safe work without refunding the action or awarding progress");
            CompleteRobbery(host);Check(safeProgress==0&&!host.bag,"completion also rejects a player away from the safe");
            Window(Role.Outlaw);safeProgress=3;Cmd("rob");
            Check(!rounds.HasSpent(host.id)&&!host.bag,"money cannot be collected remotely from an open safe");
            Place(host,SafeApproach);Cmd("rob");host.busyUntil=Time.time-.01f;TickServer();
            Check(host.bag&&host.storyProgress.HasFlag(StoryProgress.SafeWork),"finishing work beside an open safe collects the money");
            Broadcast();UpdateInteractionHint();Check(!InteractionHint.Contains("H  /"),"carrier is not prompted to collect again");
            Window(Role.Sheriff);Place(host,SafeApproach);Broadcast();UpdateInteractionHint();
            Check(NearSafe&&InteractionHint=="C  /  inspect this safe","nearby Sheriff receives the physical safe prompt");
            Window(Role.Outlaw);Place(host,SafeApproach);Broadcast();UpdateInteractionHint();
            Check(NearSafe&&InteractionHint=="H  /  work this safe or collect the money","nearby Outlaw receives the physical safe prompt");
            Window(Role.Sheriff);bot.botThink=Time.time-1;bot.controller.enabled=true;
            float until=Time.time+12;var start=bot.body.transform.position;
            while(Time.time<until&&!AtSafe(bot))
            {TickBotMovement(bot);yield return null;}
            Check(AtSafe(bot)&&Vector3.Distance(start,bot.body.transform.position)>3,"practice Outlaw walks from its stool to the real safe; start="+start+" end="+bot.body.transform.position+" seated="+bot.seat+" path="+bot.botMoveWorld+" wants="+BotWantsSafe(bot));
            TickPracticeBots();Check(bot.busyAction=="rob","practice Outlaw begins safe work only after arrival");
            yield return VerifyHeist();
            Debug.Log("SALOON_SAFE_ACCESS PASS "+checks+" checks plus existing heist regression");
            VerificationPassed=VerificationFinished=true;
        }
    }
}
#endif
