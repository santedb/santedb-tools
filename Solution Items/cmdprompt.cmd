@echo off
set path=%path%;%cd%
cd "%1"

echo SanteDB Software Development Kit 3.0
echo =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
echo.
echo There are several tools which are useful for debugging SanteDB:
echo.
echo	sdb-bb		-	A tool for extracting files from connected Android devices (requires ADB on path)
echo	adb-ade		-	A tool which allows you to debug your applets in real time in an edit/save/refresh cycle
echo	sdb-brd		-	Debugging tool for business rules and clinical protocols
echo	sdb-vmu		-	View Model Utility for generating JavaScript or C# serializers
echo	sdb-vocab		-	A tool for importing vocabulary into Datasets
echo	pakman		-	A tool for packaging your applet files for distribution
echo Successfully added to path..