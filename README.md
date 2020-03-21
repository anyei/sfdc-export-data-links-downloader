# sfdc-export-data-links-downloader
When big orgs have a lot of data to export, you can rely on this tool to automatically download all the links provided by data export page from salesforce.

### Configuration

* **baseUrl** The instance url of your salesforce org.
* **username** The salesforce user name to use.
* **pass** The password for the user.
* **dataExportPagePath** is the endpoint for the export page, default value is "/ui/setup/export/DataExportPage/d" unless salesforce changes that
* **MaxParallel** This is the total number of files to download at the same time.
* **startFromFileNumber** This number represents a row from which we will start downloading the file. This is in case there are so many files to download we have to close the program and resume it later, you may provide a none zero based index to start from.
* **downloadFolder** The folder where the files will be copied finally.
* **tempFolder** The temporal folder while the files are bieng downloaded they are put here.
* **reportDownloadDelay** The refresh rate of the console screen. This an integer number in seconds.
