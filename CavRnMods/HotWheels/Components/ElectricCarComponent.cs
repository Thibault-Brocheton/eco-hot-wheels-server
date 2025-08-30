namespace CavRnMods.HotWheels
{
    using Eco.Core.Controller;
    using Eco.Gameplay.Components;
    using Eco.Gameplay.Objects;
    using Eco.Shared.Localization;
    using Eco.Shared.Serialization;
    using System;

    [Serialized]
    [RequireComponent(typeof(VehicleComponent))]
    [RequireComponent(typeof(StatusComponent))]
    [CreateComponentTabLoc("Electric Car"), HasIcon("ElectricCarComponent")]
    public class ElectricCarComponent : WorldObjectComponent
    {
        public override WorldObjectComponentClientAvailability Availability => WorldObjectComponentClientAvailability.Always;
        private StatusElement status;
        private VehicleComponent vehicle;

        [Notify] public bool IsCharging { get; set; }
        public float CurrentChargeSpeed { get; set; }
        public float BatteryPercent => (float)Math.Round(this.CurrentWatts / this.MaxWatts * 100, 2);
        private float ConsumptionPerSecond { get; set; }
        public float MaxWatts {  get; private set; }
        [Serialized, Notify] public float CurrentWatts { get; set; }

        public void Initialize(float maxWatts, float joulesPerSecond)
        {
            this.MaxWatts = maxWatts;
            this.ConsumptionPerSecond = joulesPerSecond;

            this.status = this.Parent.GetComponent<StatusComponent>().CreateStatusElement();
            this.vehicle = this.Parent.GetComponent<VehicleComponent>();
            this.vehicle.SetAdditionalDrivableCheck(() => !(this.IsCharging || this.CurrentWatts == 0));
            this.Subscribe(nameof(this.CurrentWatts), this.NotifyDrivingChange);
            this.Subscribe(nameof(this.IsCharging), this.NotifyDrivingChange);
        }

        void NotifyDrivingChange() => this.vehicle.Changed(nameof(VehicleComponent.Drivable));

        public override void Tick() => this.Tick(this.Parent.SimTickDelta());
        public void Tick(float deltaTime)
        {
            base.Tick();

            if (!this.IsCharging)
            {
                if (!this.vehicle.IsMoving)
                {
                    this.CurrentWatts -= this.ConsumptionPerSecond * deltaTime / 432;
                }
                else
                {
                    this.CurrentWatts -= this.ConsumptionPerSecond * deltaTime;
                }

                if (this.CurrentWatts <= 0) this.CurrentWatts = 0;
            }
        }

        public override void LateTick()
        {
            if (this.IsCharging)
            {
                this.vehicle.OutOfFuel = true;
            }
            else
            {
                this.vehicle.OutOfFuel = this.CurrentWatts == 0;
            }

            this.Parent.SetAnimatedState("BatteryPercent", $"{Math.Round(this.BatteryPercent)}%");
            this.Parent.SetAnimatedState("CurrentChargeSpeed", this.CurrentChargeSpeed > 0 ? $"{Math.Round(this.CurrentChargeSpeed / 1000, 1)}kW" : "");
            this.Parent.SetAnimatedState("IsCharging", this.IsCharging);
            this.status.SetStatusMessage(
                !(this.IsCharging || this.CurrentWatts == 0),
                Localizer.DoStr($"Battery is ({this.BatteryPercent}%)"),
                this.IsCharging
                    ? Localizer.DoStr($"Battery is charging... ({this.BatteryPercent}%)")
                    : Localizer.DoStr("Battery is empty!")
                );
        }
    }
}
