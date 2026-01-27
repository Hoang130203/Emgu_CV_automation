# 🚀 Hướng Dẫn Build & Run

## ⚠️ **QUAN TRỌNG: Đóng App Trước Khi Build!**

Nếu app đang chạy, build sẽ bị lỗi:
```
Error MSB3027: Could not copy... file is locked
```

**Giải pháp:** Đóng app WPF trước khi build!

---

## 📦 **Build Project**

### **Build toàn bộ solution:**
```bash
dotnet build GameAutomation.sln
```

### **Build chỉ WPF project:**
```bash
dotnet build src/UI/GameAutomation.UI.WPF/GameAutomation.UI.WPF.csproj
```

### **Build với cấu hình Release:**
```bash
dotnet build GameAutomation.sln -c Release
```

---

## ▶️ **Run Application**

### **Option 1: Run từ solution root**
```bash
dotnet run --project src/UI/GameAutomation.UI.WPF/GameAutomation.UI.WPF.csproj
```

### **Option 2: Navigate vào folder trước**
```bash
cd src/UI/GameAutomation.UI.WPF
dotnet run
```

### **Option 3: Chạy file .exe trực tiếp**
```bash
# Sau khi build
.\src\UI\GameAutomation.UI.WPF\bin\Debug\net9.0-windows\GameAutomation.UI.WPF.exe
```

---

## 📦 **Publish Single .exe (Deployment)**

### **Publish thành 1 file .exe duy nhất:**

```bash
dotnet publish src/UI/GameAutomation.UI.WPF/GameAutomation.UI.WPF.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -o publish
```

### **Kết quả:**
```
publish/
└── GameAutomation.UI.WPF.exe  (~100-150MB)
```

### **Deploy:**
- Copy file `GameAutomation.UI.WPF.exe` sang máy khác
- Copy folder `Assets/` cùng thư mục (nếu có game images)
- Double-click để chạy!

---

## 🎯 **Các Lệnh Hữu Ích**

### **Clean build:**
```bash
dotnet clean
dotnet build
```

### **Restore packages:**
```bash
dotnet restore
```

### **Rebuild toàn bộ:**
```bash
dotnet clean
dotnet restore
dotnet build
```

### **Check dependencies:**
```bash
dotnet list package
```

### **Update packages:**
```bash
dotnet list package --outdated
```

---

## 🐛 **Troubleshooting**

### **Problem 1: File is locked**
```
Error MSB3027: Could not copy... file is locked
```
**Solution:**
1. Đóng app WPF nếu đang chạy
2. Đóng Visual Studio (nếu mở)
3. Chạy lại build

---

### **Problem 2: Missing packages**
```
Error: Package 'MaterialDesignThemes' not found
```
**Solution:**
```bash
dotnet restore
```

---

### **Problem 3: Target framework not found**
```
Error: The current .NET SDK does not support targeting .NET 9.0
```
**Solution:**
- Cài .NET 9 SDK: https://dotnet.microsoft.com/download
- Hoặc downgrade project về .NET 8

**Downgrade to .NET 8:**
Edit `GameAutomation.UI.WPF.csproj`:
```xml
<TargetFramework>net8.0-windows</TargetFramework>
```

---

### **Problem 4: EmguCV native libraries not found**
```
Error: Unable to load DLL 'cvextern'
```
**Solution:**
- Packages đã include runtime: `Emgu.CV.runtime.windows`
- Nếu vẫn lỗi, build lại với `--self-contained true`

---

## 📊 **Build Output Structure**

### **Debug Build:**
```
src/UI/GameAutomation.UI.WPF/bin/Debug/net9.0-windows/
├── GameAutomation.UI.WPF.exe
├── GameAutomation.Core.Models.dll
├── GameAutomation.Core.Services.dll
├── GameAutomation.Core.Workflows.dll
├── GameAutomation.Core.Bot.dll
├── GameAutomation.AI.*.dll
├── MaterialDesignThemes.Wpf.dll
├── Emgu.CV.dll
├── runtimes/
│   └── win-x64/
│       └── native/
│           └── *.dll (OpenCV natives)
└── ... (other dependencies)
```

### **Published Single File:**
```
publish/
└── GameAutomation.UI.WPF.exe  (All-in-one)
```

---

## 🎨 **Development Workflow**

### **1. Make changes to code**
- Edit `.cs` files (ViewModel, Services, etc.)
- Edit `.xaml` files (UI)

### **2. Build & Test**
```bash
dotnet build
dotnet run --project src/UI/GameAutomation.UI.WPF/GameAutomation.UI.WPF.csproj
```

### **3. Repeat**
- Make more changes
- Build & run again

### **4. Final Publish**
```bash
dotnet publish ... (see above)
```

---

## ⚡ **Hot Reload (Development)**

WPF hỗ trợ Hot Reload cho XAML:

1. Run app:
```bash
dotnet run --project src/UI/GameAutomation.UI.WPF/GameAutomation.UI.WPF.csproj
```

2. Edit `.xaml` files
3. Save → UI tự động update!

**Note:** C# code changes cần rebuild & restart.

---

## 📝 **Quick Commands**

### **Full Clean Build & Run:**
```bash
dotnet clean && dotnet build && dotnet run --project src/UI/GameAutomation.UI.WPF/GameAutomation.UI.WPF.csproj
```

### **Build All & Run WPF:**
```bash
dotnet build GameAutomation.sln && dotnet run --project src/UI/GameAutomation.UI.WPF/GameAutomation.UI.WPF.csproj
```

### **Publish Quick:**
```bash
dotnet publish src/UI/GameAutomation.UI.WPF/GameAutomation.UI.WPF.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

---

## ✅ **Ready to Go!**

**Bây giờ bạn có thể:**

1. ✅ Build project
2. ✅ Run WPF app với UI đẹp
3. ✅ Publish thành single .exe
4. ✅ Deploy lên máy khác

**Chúc bạn code vui vẻ! 🎉**
