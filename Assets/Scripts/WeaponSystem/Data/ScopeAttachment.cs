using UnityEngine;

[CreateAssetMenu(fileName = "Scope", menuName = "ShadowStrike/Weapons/Attachments/Scope")]
public class ScopeAttachment : AttachmentData
{
    //Scope behavior #=============#
    public bool overrideAimFOV = true;
    public float aimFOVOverride = 25f;    // tight zoom — stat resolver reads this when overrideAimFOV is true
    public GameObject scopeOverlayPrefab; // optional UI overlay when ADS

    private void OnValidate()
    {
        slot = AttachmentSlot.Scope;
    }
}
