// using UnityEngine;

// public class Stopper : MonoBehaviour
// {

//     public RobotArmController rm;
//     public Transform stopper;
//     public float stopperSpeed = 1.0f;
//     public float stopperY = 0.5f;


//     // Start is called once before the first execution of Update after the MonoBehaviour is created
//     void Start()
//     {
//         rm.OnRobotBusyChanged += HandleRobotState;
//     }

//     // Update is called once per frame
//     void HandleRobotState(bool isBusy)
//     {
//         if(isBusy) // if(isBusy == true)
//         {
//             RaiseStopper();
//         }
//         else
//         {
//             LowerStopper();
//         }
//     }

//     public void RaiseStopper()
//     {

//     }

//     public void LowerStopper()
//     {

//     }
// }
