using UnityEngine;

[CreateAssetMenu(fileName = "Muzzle", menuName = "ShadowStrike/Weapons/Attachments/Muzzle")]
public class MuzzleAttachment : AttachmentData
{
    //Visual override for muzzle flash
    public GameObject muzzleFlashPrefab;

    private void OnValidate()
    {
        slot = AttachmentSlot.Muzzle;
    }
}
