using UnityEngine;

public class FixRagdollJoints : MonoBehaviour
{
    void Awake()
    {
        var joints = GetComponentsInChildren<CharacterJoint>();

        foreach (var joint in joints)
        {
            SoftJointLimit low = joint.lowTwistLimit;
            SoftJointLimit high = joint.highTwistLimit;
            SoftJointLimit swing1 = joint.swing1Limit;
            SoftJointLimit swing2 = joint.swing2Limit;

            // Better realistic deer limits
            low.limit = -20f;
            high.limit = 20f;
            swing1.limit = 35f;
            swing2.limit = 35f;

            joint.lowTwistLimit = low;
            joint.highTwistLimit = high;
            joint.swing1Limit = swing1;
            joint.swing2Limit = swing2;

            joint.enableProjection = true;
            joint.projectionDistance = 0.1f;
            joint.projectionAngle = 5f;
        }
    }
}
