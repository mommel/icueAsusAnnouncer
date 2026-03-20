#include <windows.h>
#include <stdio.h>
#include <stdint.h>
#include <vector>
#include <string>

// --- NVAPI Definitions ---
typedef int32_t NV_S32;
typedef uint32_t NV_U32;
typedef uint8_t NV_U8;
typedef NV_S32* NV_HANDLE;
typedef NV_HANDLE NV_PHYSICAL_GPU_HANDLE;
typedef NV_S32 NV_STATUS;

#define NV_STRUCT_VERSION(STRUCT, VERSION) (((VERSION) << 16) | sizeof(STRUCT))

struct NV_I2C_INFO_V3 {
	NV_U32 version;
	NV_U32 display_mask;
	NV_U8 is_ddc_port;
	NV_U8 i2c_dev_address;
	NV_U8* i2c_reg_address;
	NV_U32 reg_addr_size;
	NV_U8* data;
	NV_U32 size;
	NV_U32 i2c_speed;
	NV_U32 i2c_speed_khz;
	NV_U8 port_id;
	NV_U32 is_port_id_set;
};

// Function Pointers
static NV_STATUS(*pNvAPI_Initialize)();
static NV_STATUS(*pNvAPI_EnumPhysicalGPUs)(NV_PHYSICAL_GPU_HANDLE*, NV_S32*);
static NV_STATUS(*pNvAPI_I2CWriteEx)(NV_PHYSICAL_GPU_HANDLE, NV_I2C_INFO_V3*, NV_U32*);
static NV_STATUS(*pNvAPI_I2CReadEx)(NV_PHYSICAL_GPU_HANDLE, NV_I2C_INFO_V3*, NV_U32*);

typedef void* (*nvapi_QueryInterface_t)(int);

static void* LoadNvApiFunc(nvapi_QueryInterface_t query, NV_U32 id) {
	return ((void* (*)(NV_U32))query)(id);
}

// --- ENE SMBus Constants ---
#define ENE_I2C_ADDR 0x67
#define ENE_APPLY_VAL 0x01
#define ENE_REG_DEVICE_NAME 0x1000
#define ENE_REG_CONFIG_TABLE 0x1C00
#define ENE_REG_DIRECT 0x8020
#define ENE_REG_MODE 0x8021
#define ENE_REG_SPEED 0x8022
#define ENE_REG_DIRECTION 0x8023
#define ENE_REG_APPLY 0x80A0
#define ENE_REG_COLORS_DIRECT_V2 0x8100
#define ENE_REG_COLORS_EFFECT_V2 0x8160

// --- Globals ---
static NV_PHYSICAL_GPU_HANDLE g_AsusGpu = nullptr;
static bool g_Initialized = false;
static int g_LedCount = 0;
static char g_DeviceName[32] = {0};
static uint8_t g_ColorBufferDirect[256];
static uint8_t g_ColorBufferEffect[256];

// --- I2C Helpers ---
bool I2cWriteWord(NV_PHYSICAL_GPU_HANDLE gpu, uint8_t deviceAddr, uint8_t cmd, uint16_t word) {
	NV_I2C_INFO_V3 info = {0};
	info.version = NV_STRUCT_VERSION(NV_I2C_INFO_V3, 3);
	info.port_id = 1;
	info.is_port_id_set = 1;
	info.i2c_speed = 0xFFFF;
	info.i2c_dev_address = (deviceAddr << 1);
	
	uint8_t data[2] = { (uint8_t)(word & 0xFF), (uint8_t)((word >> 8) & 0xFF) };
	info.reg_addr_size = 1;
	uint8_t regAddr = cmd;
	info.i2c_reg_address = &regAddr;
	info.data = data;
	info.size = 2;
	
	uint32_t unknown = 0;
	return pNvAPI_I2CWriteEx(gpu, &info, &unknown) == 0;
}

bool I2cWriteByte(NV_PHYSICAL_GPU_HANDLE gpu, uint8_t deviceAddr, uint8_t cmd, uint8_t val) {
	NV_I2C_INFO_V3 info = {0};
	info.version = NV_STRUCT_VERSION(NV_I2C_INFO_V3, 3);
	info.port_id = 1;
	info.is_port_id_set = 1;
	info.i2c_speed = 0xFFFF;
	info.i2c_dev_address = (deviceAddr << 1);
	
	uint8_t data[1] = { val };
	info.reg_addr_size = 1;
	uint8_t regAddr = cmd;
	info.i2c_reg_address = &regAddr;
	info.data = data;
	info.size = 1;
	
	uint32_t unknown = 0;
	return pNvAPI_I2CWriteEx(gpu, &info, &unknown) == 0;
}

bool I2cReadByte(NV_PHYSICAL_GPU_HANDLE gpu, uint8_t deviceAddr, uint8_t cmd, uint8_t* val) {
	NV_I2C_INFO_V3 info = {0};
	info.version = NV_STRUCT_VERSION(NV_I2C_INFO_V3, 3);
	info.port_id = 1;
	info.is_port_id_set = 1;
	info.i2c_speed = 0xFFFF;
	info.i2c_dev_address = (deviceAddr << 1);
	
	uint8_t data[1] = { 0 };
	info.reg_addr_size = 1;
	uint8_t regAddr = cmd;
	info.i2c_reg_address = &regAddr;
	info.data = data;
	info.size = 1;
	
	uint32_t unknown = 0;
	if (pNvAPI_I2CReadEx(gpu, &info, &unknown) == 0) {
		*val = data[0];
		return true;
	}
	return false;
}

bool I2cWriteBlock(NV_PHYSICAL_GPU_HANDLE gpu, uint8_t deviceAddr, uint8_t cmd, uint8_t* data, uint8_t sz) {
	NV_I2C_INFO_V3 info = {0};
	info.version = NV_STRUCT_VERSION(NV_I2C_INFO_V3, 3);
	info.port_id = 1;
	info.is_port_id_set = 1;
	info.i2c_speed = 0xFFFF;
	info.i2c_dev_address = (deviceAddr << 1);
	
	uint8_t buf[256];
	buf[0] = sz;
	memcpy(&buf[1], data, sz);
	
	info.reg_addr_size = 1;
	uint8_t regAddr = cmd;
	info.i2c_reg_address = &regAddr;
	info.data = buf;
	info.size = sz + 1;
	
	uint32_t unknown = 0;
	return pNvAPI_I2CWriteEx(gpu, &info, &unknown) == 0;
}

// --- ENE Helpers ---
uint8_t EneRead(NV_PHYSICAL_GPU_HANDLE gpu, uint16_t reg) {
	uint16_t regSwap = ((reg << 8) & 0xFF00) | ((reg >> 8) & 0x00FF);
	I2cWriteWord(gpu, ENE_I2C_ADDR, 0x00, regSwap);
	uint8_t val = 0;
	I2cReadByte(gpu, ENE_I2C_ADDR, 0x81, &val);
	return val;
}

void EneWrite(NV_PHYSICAL_GPU_HANDLE gpu, uint16_t reg, uint8_t val) {
	uint16_t regSwap = ((reg << 8) & 0xFF00) | ((reg >> 8) & 0x00FF);
	I2cWriteWord(gpu, ENE_I2C_ADDR, 0x00, regSwap);
	I2cWriteByte(gpu, ENE_I2C_ADDR, 0x01, val);
}

void EneWriteBlock(NV_PHYSICAL_GPU_HANDLE gpu, uint16_t reg, uint8_t* data, uint8_t sz) {
	uint16_t regSwap = ((reg << 8) & 0xFF00) | ((reg >> 8) & 0x00FF);
	I2cWriteWord(gpu, ENE_I2C_ADDR, 0x00, regSwap);
	I2cWriteBlock(gpu, ENE_I2C_ADDR, 0x03, data, sz);
}

// --- Exported API ---
extern "C" __declspec(dllexport) int AuraGpu_Connect() {
	if (g_Initialized) return g_LedCount;

	HMODULE nvapi = LoadLibraryA(sizeof(void*) == 4 ? "nvapi.dll" : "nvapi64.dll");
	if (!nvapi) return 0;

	auto query = (nvapi_QueryInterface_t)GetProcAddress(nvapi, "nvapi_QueryInterface");
	if (!query) return 0;

	pNvAPI_Initialize = (NV_STATUS(*)())LoadNvApiFunc(query, 0x0150E828);
	pNvAPI_EnumPhysicalGPUs = (NV_STATUS(*)(NV_PHYSICAL_GPU_HANDLE*, NV_S32*))LoadNvApiFunc(query, 0xE5AC921F);
	pNvAPI_I2CWriteEx = (NV_STATUS(*)(NV_PHYSICAL_GPU_HANDLE, NV_I2C_INFO_V3*, NV_U32*))LoadNvApiFunc(query, 0x283AC65A);
	pNvAPI_I2CReadEx = (NV_STATUS(*)(NV_PHYSICAL_GPU_HANDLE, NV_I2C_INFO_V3*, NV_U32*))LoadNvApiFunc(query, 0x4D7B0709);

	if (!pNvAPI_Initialize || !pNvAPI_EnumPhysicalGPUs || !pNvAPI_I2CWriteEx || !pNvAPI_I2CReadEx) return 0;

	if (pNvAPI_Initialize() != 0) return 0;

	NV_PHYSICAL_GPU_HANDLE gpus[64];
	NV_S32 gpuCount = 0;
	if (pNvAPI_EnumPhysicalGPUs(gpus, &gpuCount) != 0) return 0;

	// Find the ENE device
	for (int i = 0; i < gpuCount; i++) {
		char devName[17] = {0};
		for (int j = 0; j < 16; j++) {
			devName[j] = EneRead(gpus[i], ENE_REG_DEVICE_NAME + j);
		}
		
		if (strncmp(devName, "AUMA", 4) == 0 || strncmp(devName, "LED", 3) == 0) {
			g_AsusGpu = gpus[i];
			strncpy(g_DeviceName, devName, sizeof(g_DeviceName) - 1);
			g_LedCount = EneRead(gpus[i], ENE_REG_CONFIG_TABLE + 0x03);
			if (g_LedCount <= 0 || g_LedCount > 80) g_LedCount = 24; // Fallback
			g_Initialized = true;
			
			// Initialize direct mode (SetDirect)
			EneWrite(g_AsusGpu, ENE_REG_DIRECT, 1);
			EneWrite(g_AsusGpu, ENE_REG_APPLY, ENE_APPLY_VAL);
			
			memset(g_ColorBufferDirect, 0, sizeof(g_ColorBufferDirect));
			memset(g_ColorBufferEffect, 0, sizeof(g_ColorBufferEffect));
			return g_LedCount;
		}
	}

	return 0;
}

extern "C" __declspec(dllexport) void AuraGpu_SetColor(int zone, uint8_t r, uint8_t g, uint8_t b) {
	if (!g_Initialized || zone >= g_LedCount) return;
	
	// ENE expects R, B, G
	g_ColorBufferDirect[zone * 3 + 0] = r;
	g_ColorBufferDirect[zone * 3 + 1] = b;
	g_ColorBufferDirect[zone * 3 + 2] = g;
	
	g_ColorBufferEffect[zone * 3 + 0] = r;
	g_ColorBufferEffect[zone * 3 + 1] = b;
	g_ColorBufferEffect[zone * 3 + 2] = g;
}

extern "C" __declspec(dllexport) void AuraGpu_Apply() {
	if (!g_Initialized) return;
	
	int totalBytes = g_LedCount * 3;
	int sentBytes = 0;
	
	// Write direct register
	while (sentBytes < totalBytes) {
		int toSend = totalBytes - sentBytes;
		if (toSend > 3) toSend = 3; // Max block size is 3 for ENE SMBus
		EneWriteBlock(g_AsusGpu, ENE_REG_COLORS_DIRECT_V2 + sentBytes, &g_ColorBufferDirect[sentBytes], toSend);
		sentBytes += toSend;
	}
	
	sentBytes = 0;
	// Write effect register
	while (sentBytes < totalBytes) {
		int toSend = totalBytes - sentBytes;
		if (toSend > 3) toSend = 3;
		EneWriteBlock(g_AsusGpu, ENE_REG_COLORS_EFFECT_V2 + sentBytes, &g_ColorBufferEffect[sentBytes], toSend);
		sentBytes += toSend;
	}
	
	// Apply
	EneWrite(g_AsusGpu, ENE_REG_APPLY, ENE_APPLY_VAL);
}

extern "C" __declspec(dllexport) void AuraGpu_Disconnect() {
	g_Initialized = false;
	g_AsusGpu = nullptr;
}

extern "C" __declspec(dllexport) void AuraGpu_GetName(char* outName, int maxLen) {
	if (g_Initialized && outName && maxLen > 0) {
		strncpy(outName, g_DeviceName, maxLen - 1);
		outName[maxLen - 1] = '\0';
	}
}
