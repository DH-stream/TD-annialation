using System.Collections;
using UnityEngine;

namespace TDAnnihilation
{
    public sealed class TDArcherTowerDrawAnimation : MonoBehaviour
    {
        [SerializeField] private Transform aimPivot;
        [SerializeField] private Transform bolt;
        [SerializeField] private Transform shotOrigin;
        [SerializeField] private Vector3 aimOffsetEuler = new Vector3(0f, 90f, 0f);
        [SerializeField] private float aimSpeed = 8f;
        [SerializeField] private Vector3 drawDirection = Vector3.right;
        [SerializeField] private float drawDistance = 0.001f;
        [SerializeField] private float drawDuration = 0.16f;
        [SerializeField] private float releaseDuration = 0.08f;
        [SerializeField] private float settleDuration = 0.14f;

        private Vector3 restPosition;
        private Coroutine shotRoutine;

        private void Awake()
        {
            if (aimPivot == null) aimPivot = transform;
            if (bolt == null) bolt = FindChild(transform, "DrawBolt");
            if (bolt != null) restPosition = bolt.localPosition;
        }

        public Vector3 ShotPosition => shotOrigin != null ? shotOrigin.position : aimPivot.position + aimPivot.forward * 0.45f;
        public Quaternion ShotRotation => Quaternion.LookRotation(aimPivot.TransformDirection(Vector3.left), Vector3.up);

        public void AimAt(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - aimPivot.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized) * Quaternion.Euler(aimOffsetEuler);
            aimPivot.rotation = Quaternion.Slerp(aimPivot.rotation, targetRotation, Time.deltaTime * aimSpeed);
        }

        public void PlayShot()
        {
            if (bolt == null) return;
            if (shotRoutine != null) StopCoroutine(shotRoutine);
            shotRoutine = StartCoroutine(PlayShotRoutine());
        }

        private IEnumerator PlayShotRoutine()
        {
            Vector3 direction = drawDirection.sqrMagnitude > 0.001f ? drawDirection.normalized : Vector3.back;
            Vector3 drawnPosition = restPosition + direction * drawDistance;
            yield return MoveAssembly(restPosition, drawnPosition, drawDuration);
            yield return MoveAssembly(drawnPosition, restPosition, releaseDuration);
            Vector3 overshootPosition = restPosition + direction * (drawDistance * 0.08f);
            yield return MoveAssembly(restPosition, overshootPosition, settleDuration * 0.4f);
            yield return MoveAssembly(overshootPosition, restPosition, settleDuration * 0.6f);
            shotRoutine = null;
        }

        private IEnumerator MoveAssembly(Vector3 from, Vector3 to, float duration)
        {
            if (duration <= 0f)
            {
                bolt.localPosition = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                bolt.localPosition = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }
            bolt.localPosition = to;
        }

        private static Transform FindChild(Transform parent, string childName)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
                if (child.name == childName) return child;
            return null;
        }

        private void OnDisable()
        {
            if (shotRoutine != null) StopCoroutine(shotRoutine);
            shotRoutine = null;
            if (bolt != null) bolt.localPosition = restPosition;
        }
    }
}
