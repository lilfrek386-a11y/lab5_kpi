using Energy_Project.Services.Interfaces;
using Moq;
using Energy_Project.Models;
using Energy_Project.Services;
using Xunit;

namespace SmartHomeTests
{
	public class EnergyMonitorServiceTests
	{
		private readonly Mock<IDeviceRepository> _deviceRepo = new();
		private readonly Mock<IEnergyPlanRepository> _planRepo = new();
		private readonly Mock<INotificationService> _notify = new();
		private readonly EnergyMonitorService _energyService;

		public EnergyMonitorServiceTests()
		{
			_energyService = new EnergyMonitorService(_deviceRepo.Object, _planRepo.Object, _notify.Object);
		}
		/// <summary>
		/// Перевіряє розрахунок поточної потужності (кВт) 
		/// для різних сценаріїв.
		/// </summary>
		[Theory]
		[InlineData(new double[] { 1000, 1500, 2000 }, 4.5)]
		[InlineData(new double[] { 1000, 500 }, 1.5)]
		[InlineData(new double[] { }, 0.0)]
		public void CalculateCurrentUsageKwh_WithVariousScenarios_ReturnsCorrectSum(
			double[] onDevicesWatts, double expectedPowerInKwh)
		{
			var allDevices = new List<Device>();
			int idCounter = 1;

			foreach (var watts in onDevicesWatts)
			{
				allDevices.Add(new Device
				{
					Id = idCounter++,
					IsOn = true,
					PowerUsageWatts = watts
				});
			}

			_deviceRepo
				.Setup(repo => repo.GetAll())
				.Returns(allDevices);


			double actualPower = _energyService.CalculateCurrentUsageKwh();

			Assert.Equal(expectedPowerInKwh, actualPower);
		}

		/// <summary>
		/// Перевіряє, що SendAlert() викликається, 
		/// коли поточне споживання перевищує ліміт плану.
		/// </summary>
		[Fact]
		public void CheckForOverload_WhenUsageExceedsLimit_ShouldSendAlert()
		{
			var devices = new List<Device> { new Device { IsOn = true, PowerUsageWatts = 3500 } };
			_deviceRepo.Setup(repo => repo.GetAll()).Returns(devices);

			var plan = new EnergyPlan { DailyLimitKwh = 3.0 };
			_planRepo.Setup(repo => repo.GetCurrentPlan()).Returns(plan);

			_notify.Setup(n => n.SendAlert(It.IsAny<string>()));

			_energyService.CheckForOverload();

			_notify.Verify(
				n => n.SendAlert("Overload detected: 3,5 kWh used!"),
				Times.Once()
			);
		}

		/// <summary>
		/// Перевіряє, що SendAlert() НЕ викликається, 
		/// коли поточне споживання В МЕЖАХ ліміту плану.
		/// </summary>
		[Fact]
		public void CheckForOverload_WhenUsageIsWithinLimit_ShouldNotSendAlert()
		{
			var devices = new List<Device> { new Device { IsOn = true, PowerUsageWatts = 2500 } };
			_deviceRepo.Setup(repo => repo.GetAll()).Returns(devices);

			var plan = new EnergyPlan { DailyLimitKwh = 3.0 };
			_planRepo.Setup(repo => repo.GetCurrentPlan()).Returns(plan);

			_energyService.CheckForOverload();

			_notify.Verify(
				n => n.SendAlert(It.IsAny<string>()),
				Times.Never()
			);
		}

		/// <summary>
		/// Перевіряє, що сервіс правильно оновлює ліміт 
		/// в об'єкті плану та викликає UpdatePlan() репозиторію.
		/// </summary>
		[Fact]
		public void UpdateEnergyLimit_WhenCalled_ShouldUpdatePlan()
		{
			double newLimit = 50.0; 

			var plan = new EnergyPlan { DailyLimitKwh = 10.0 };
			_planRepo.Setup(repo => repo.GetCurrentPlan()).Returns(plan);

			_planRepo.Setup(repo => repo.UpdatePlan(It.IsAny<EnergyPlan>()));

			_energyService.UpdateEnergyLimit(newLimit);

			_planRepo.Verify(
				repo => repo.UpdatePlan(plan),
				Times.Once()
			);

			Assert.Equal(newLimit, plan.DailyLimitKwh);
		}


		/// <summary>
		/// Перевіряє, що сервіс правильно оновлює ліміт якщо число від'ємне 
		/// в об'єкті плану та викликає UpdatePlan() репозиторію.
		/// </summary>

		[Fact]
		public void UpdateEnergyLimit_WhenCalled_ShoulldUpdatePlanV2()
		{
			double newLimit = -10;

			var plan = new EnergyPlan { DailyLimitKwh = 10.0 };
			_planRepo.Setup(repo => repo.GetCurrentPlan()).Returns(plan);

			_planRepo.Setup(repo => repo.UpdatePlan(It.IsAny<EnergyPlan>()));

			_energyService.UpdateEnergyLimit(newLimit);

			_planRepo.Verify(
				repo => repo.UpdatePlan(plan),
				Times.Once()
			);

			Assert.Equal(newLimit, plan.DailyLimitKwh);
		}

	}

}
