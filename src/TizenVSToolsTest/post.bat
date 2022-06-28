dotnet tool install --global coverlet.console --version 1.7.2
if "%1"=="Debug" coverlet .\bin\Debug\netcoreapp3.1\TizenVSToolsTest.dll  --target "dotnet"   --targetargs "test --no-build -c Debug" --exclude-by-file "**/Data/ToolsPathInfo.cs" --exclude-by-file "**/obj/**/*" --exclude-by-file "**/Utilities/**/*" --exclude-by-file "**/ViewAndUI/**/*" --exclude-by-file "**/DebugBridge/**/*" --exclude-by-file "**/ExternalTools/**/*" --output "./coverage-reports/" --format "opencover" & dotnet test --no-build -c Debug -l:trx;LogFileName=./Output.xml
if "%1"=="Release" coverlet .\bin\Release\netcoreapp3.1\TizenVSToolsTest.dll  --target "dotnet"   --targetargs "test --no-build -c Release" --exclude-by-file "**/Data/ToolsPathInfo.cs" --exclude-by-file "**/obj/**/*" --exclude-by-file "**/Utilities/**/*" --exclude-by-file "**/ViewAndUI/**/*" --exclude-by-file "**/DebugBridge/**/*" --exclude-by-file "**/ExternalTools/**/*" --output "./coverage-reports/" --format "opencover" & dotnet test --no-build -c Release -l:trx;LogFileName=./Output.xml