using UnityEngine;

namespace Nightblade
{
    // Plain tracking keeps the large test course visible. Not a production camera.
    public sealed class MovementLabCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float verticalOffset = 0.75f;

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = new Vector3(
                target.position.x, target.position.y + verticalOffset, transform.position.z);
        }
    }
}
