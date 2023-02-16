using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class TestAgent : Agent
{
    //초기화 작업을 위해 한번 호출되는 메소드
    public override void Initialize()
    {

    }

    //에피소드(학습단위)가 시작할때마다 호출
    public override void OnEpisodeBegin()
    {

    }

    //환경 정보를 관측 및 수집해 정책 결정을 위해 브레인에 전달하는 메소드
    public override void CollectObservations(VectorSensor sensor)
    {

    }

    //브레인(정책)으로 부터 전달 받은 행동을 실행하는 메소드
    public override void OnActionReceived(ActionBuffers actions)
    {
        
    }

    //개발자(사용자)가 직접 명령을 내릴때 호출하는 메소드(주로 테스트용도 또는 모방학습에 사용)
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        
    }
}
