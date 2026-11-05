using System.Collections.Generic;
using UnityEngine;

namespace Restart
{
    [DefaultExecutionOrder(-100)]
    public sealed class ProjectileEmitter2D : AttemptMechanism
    {
        [SerializeField] private Projectile2D projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField, Min(0), Tooltip("每次尝试开始后，等待多少秒首次发射。")]
        private float initialDelay = 1.5f;
        [SerializeField, Min(0.02f), Tooltip("两轮连射开始之间的秒数；不会短于一轮连射所需的时间。")]
        private float fireInterval = 3.5f;
        [SerializeField, Min(1), Tooltip("每轮连射的子弹数，设为 1 即为定时单发。")]
        private int bulletsPerBurst = 3;
        [SerializeField, Min(0.02f), Tooltip("同一轮内相邻两发子弹的间隔。")]
        private float burstSpacing = 0.32f;
        [SerializeField, Min(0.1f), Tooltip("子弹每秒移动的距离；发射方向为 Muzzle 的红色 X 轴。")]
        private float bulletSpeed = 7f;
        private readonly List<Projectile2D> liveBullets = new List<Projectile2D>();
        private int tick;
        private int nextShotTick;
        private int burstStartTick;
        private int shotInBurst;
        public event System.Action<ProjectionActor> BlockedByProjection;
        internal void ReportProjectionBlock(ProjectionActor projection) => BlockedByProjection?.Invoke(projection);

        protected override void ResetAttempt()
        {
            foreach (var bullet in liveBullets) if (bullet != null) bullet.Retire();
            liveBullets.Clear();
            tick = shotInBurst = 0;
            burstStartTick = nextShotTick = Mathf.CeilToInt(initialDelay / Time.fixedDeltaTime);
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            foreach (var bullet in liveBullets) if (bullet != null) bullet.Retire();
            liveBullets.Clear();
        }
        private void FixedUpdate()
        {
            if (projectilePrefab != null && tick >= nextShotTick)
            {
                var source = muzzle != null ? muzzle : transform;
                var bullet = Instantiate(projectilePrefab, source.position, source.rotation);
                bullet.Launch(source.right, bulletSpeed, this);
                liveBullets.RemoveAll(item => item == null);
                liveBullets.Add(bullet);
                int spacingTicks = Mathf.Max(1, Mathf.CeilToInt(burstSpacing / Time.fixedDeltaTime));
                shotInBurst++;
                if (shotInBurst < Mathf.Max(1, bulletsPerBurst)) nextShotTick += spacingTicks;
                else
                {
                    shotInBurst = 0;
                    burstStartTick += Mathf.Max(spacingTicks * Mathf.Max(1, bulletsPerBurst), Mathf.CeilToInt(fireInterval / Time.fixedDeltaTime));
                    nextShotTick = burstStartTick;
                }
            }
            tick++;
        }
        private void OnDrawGizmosSelected()
        {
            var source = muzzle != null ? muzzle : transform;
            Gizmos.color = new Color(1, 0.45f, 0.2f);
            Gizmos.DrawRay(source.position, source.right * 2f);
            Gizmos.DrawWireSphere(source.position, 0.14f);
        }
    }
}
