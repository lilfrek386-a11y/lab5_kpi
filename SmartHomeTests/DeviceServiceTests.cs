using Energy_Project.Services.Interfaces;
using Moq;
using Energy_Project.Models;
using Energy_Project.Services;
using Xunit;

namespace SmartHomeTests
{
    public class DeviceServiceTests
    {
        private readonly Mock<IDeviceRepository> _deviceRepo = new();

		/// <summary>
		/// Перевірка методу ToggleDevice класу DeviceService
		/// в тесті перевіряється який результат повертає фукнція, який приймає прилад 
		/// та як на це реагує клас репозиторію
		/// </summary>
		[Theory] 
	
		[InlineData(1, false, true)] 
		[InlineData(2, true, false)] 
		public void ToggleDevice_WhenDeviceExists_ShouldChangeState_And_CallUpdate(
			int deviceId, bool initialIsOn, bool targetIsOn)
		{
			var device = new Device
			{
				Id = deviceId,       
				IsOn = initialIsOn,  
				Name = "Test Device"
			};

			_deviceRepo
				.Setup(repo => repo.GetById(deviceId)) 
				.Returns(device);

			_deviceRepo
				.Setup(repo => repo.Update(It.IsAny<Device>()));

			var deviceService = new DeviceService(_deviceRepo.Object);

			bool result = deviceService.ToggleDevice(deviceId, targetIsOn); 

			Assert.Equal(targetIsOn, result);

			Assert.Equal(targetIsOn, device.IsOn);

			_deviceRepo.Verify(
				repo => repo.Update(device),
				Times.Once()
			);
		}

		/// Перевірка методу ToggleDevice класу DeviceService
		/// за умови якщо передається неіснуюче id для device
		/// </summary>
		[Fact]
		public void ToggleDevice_WhenDeviceNotExists_ShouldMakeException()
		{
			var deviceService = new DeviceService(_deviceRepo.Object);

			Exception exception = null;

			Assert.Throws<ArgumentException>(() =>
			{
				deviceService.ToggleDevice(0, true);
			});
		}

		/// <summary>
		/// Перевірка методу GetActiveDevices класу DeviceService
		/// тест повинен перевірити чи всі включені об'єкти були повернуті
		/// та чи немає в цьому списку вимкнутого об'єкту
		/// </summary>
		[Fact]
		public void GetActiveDevices_WhenCalled_ReturnsOnlyOnDevices()
		{
			var device1_Off = new Device { Id = 1, IsOn = false, Name = "Test1" };
			var device2_On = new Device { Id = 2, IsOn = true, Name = "Test2" };
			var device3_On = new Device { Id = 3, IsOn = true, Name = "Test3" };

			var allDevices = new List<Device>
			{
				device1_Off,
				device2_On,
				device3_On
			};

			_deviceRepo
				.Setup(repo => repo.GetAll())
				.Returns(allDevices);

			var deviceService = new DeviceService(_deviceRepo.Object);

			var activeDevices = deviceService.GetActiveDevices();

			Assert.NotEmpty(activeDevices);

			Assert.Equal(2, activeDevices.Count());

			Assert.Contains(device2_On, activeDevices);

			Assert.Contains(device3_On, activeDevices);
	
			Assert.DoesNotContain(device1_Off, activeDevices);
		}



		/// <summary>
		/// Перевірка методу GetActiveDevices класу DeviceService
		/// тест повинен що список порожній, тому що всі об'єкти вимкнені
		/// </summary>
		[Fact]
		public void GetActiveDevices_WhenCalled_ReturnsOnlyOnDevicesV2()
		{
			var device1_Off = new Device { Id = 1, IsOn = false, Name = "Test1" };
			var device2_On = new Device { Id = 2, IsOn = false, Name = "Test2" };
			var device3_On = new Device { Id = 3, IsOn = false, Name = "Test3" };

			var allDevices = new List<Device>
			{
				device1_Off,
				device2_On,
				device3_On
			};

			_deviceRepo
				.Setup(repo => repo.GetAll())
				.Returns(allDevices);

			var deviceService = new DeviceService(_deviceRepo.Object);

			var activeDevices = deviceService.GetActiveDevices();

			Assert.Empty(activeDevices);
		}
	}
}
