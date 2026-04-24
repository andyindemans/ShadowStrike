using UnityEngine;

//Small shared helper — kept tiny, intentionally no file-per-firer duplication.
public static class Spread
{
    public static Vector3 Apply(Vector3 direction, float spreadDegrees)
    {
        if (spreadDegrees <= 0f) return direction.normalized;
        float yaw = Random.Range(-spreadDegrees, spreadDegrees);
        float pitch = Random.Range(-spreadDegrees, spreadDegrees);
        Quaternion baseRot = Quaternion.LookRotation(direction);
        Quaternion offset = Quaternion.Euler(pitch, yaw, 0f);
        return (baseRot * offset) * Vector3.forward;
    }
}
