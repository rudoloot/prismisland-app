using System;

namespace PrismIsland.Domain
{
    public class WeaponModel
    {
        public string WeaponId { get; private set; }
        public int CurrentAmmo { get; private set; }
        public int MaxAmmo { get; private set; }
        public float FireRate { get; private set; }
        public float Damage { get; private set; }
        public float AttackRange { get; private set; }
        public float AttackAngle { get; private set; }
        public bool IsRanged { get; private set; }

        public float NextFireTime { get; private set; }
        public bool IsReloading { get; private set; }
        public float ReloadTimer { get; private set; }
        public float ReloadDuration { get; private set; }

        public event Action<int> OnAmmoChanged;
        public event Action<bool> OnReloadStateChanged;

        public WeaponModel(string weaponId, int maxAmmo, float fireRate, float damage, float attackRange, float attackAngle, bool isRanged, float reloadDuration = 1.5f)
        {
            WeaponId = weaponId;
            MaxAmmo = maxAmmo;
            FireRate = fireRate;
            Damage = damage;
            AttackRange = attackRange;
            AttackAngle = attackAngle;
            IsRanged = isRanged;
            ReloadDuration = reloadDuration;

            CurrentAmmo = maxAmmo;
            NextFireTime = 0f;
            IsReloading = false;
        }

        public void Update(float deltaTime, float currentTime, int availableBulletsInInventory)
        {
            if (IsReloading)
            {
                ReloadTimer += deltaTime;
                if (ReloadTimer >= ReloadDuration)
                {
                    IsReloading = false;
                    CurrentAmmo = Math.Min(MaxAmmo, availableBulletsInInventory);
                    OnReloadStateChanged?.Invoke(false);
                    OnAmmoChanged?.Invoke(CurrentAmmo);
                }
            }
            else if (IsRanged && CurrentAmmo <= 0 && availableBulletsInInventory > 0)
            {
                StartReload();
            }
        }

        public void StartReload()
        {
            if (IsReloading) return;
            IsReloading = true;
            ReloadTimer = 0f;
            OnReloadStateChanged?.Invoke(true);
        }

        public bool CanAttack(float currentTime)
        {
            if (IsReloading) return false;
            if (currentTime < NextFireTime) return false;
            if (IsRanged && CurrentAmmo <= 0) return false;
            return true;
        }

        public void RecordAttack(float currentTime)
        {
            float cooldown = FireRate > 0f ? (1f / FireRate) : 0f;
            NextFireTime = currentTime + cooldown;
            if (IsRanged)
            {
                CurrentAmmo--;
                OnAmmoChanged?.Invoke(CurrentAmmo);
            }
        }
    }
}
