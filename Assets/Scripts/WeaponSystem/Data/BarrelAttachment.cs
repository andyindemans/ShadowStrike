using UnityEngine;

[CreateAssetMenu(fileName = "Barrel", menuName = "ShadowStrike/Weapons/Attachments/Barrel")]
public class BarrelAttachment : AttachmentData
{
    private void OnValidate()
    {
        slot = AttachmentSlot.Barrel;
    }
}
