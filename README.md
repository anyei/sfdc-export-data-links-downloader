# sfdc-export-data-links-downloader
When big orgs have a lot of data to export, you can rely on this tool to automatically download all the links provided by data export page from salesforce.

### Usage

Just execute the program with dotnet as follow, make sure the configuration file appsettings.json has the right values:

```bash
$ dotnet sfdc-export-data-links-downloader.dll
```
### Demo
![Demo ](https://raw.githubusercontent.com/anyei/sfdc-export-data-links-downloader/master/bin/export-demo.gif)

### Configuration

* **baseUrl** The instance url of your salesforce org.
* **isSandbox** If the credentials are for sandbox login.
* **username** The salesforce user name to use.
* **pass** The password for the user.
* **dataExportPagePath** is the endpoint for the export page, default value is "/ui/setup/export/DataExportPage/d" unless salesforce changes that
* **MaxParallel** This is the total number of files to download at the same time.
* **startFromFileNumber** This number represents a row from which we will start downloading the file. This is in case there are so many files to download we have to close the program and resume it later, you may provide a none zero based index to start from.
* **downloadFolder** The folder where the files will be copied finally.
* **tempFolder** The temporal folder while the files are bieng downloaded they are put here.
* **reportDownloadDelay** The refresh rate of the console screen. This an integer number in seconds.
* **tableContentFile** If you have an html table previously downloaded from the DataExport page from salesforce setup, you may use it by putting its content in a file. This is the file path.

Every config parameter is located in the appsettings.json file. They can be overriden when executing the program by passing the name of the parameter, all in lower case, with the "--" prefix.

```bash
$ dotnet sfdc-export-data-links-downloader.dll --username anyei@anyei.com --pass mypass --maxparallel 10 -- startfromfilenumber 2000
```
