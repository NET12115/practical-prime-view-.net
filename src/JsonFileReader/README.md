﻿# Implementation notes

* This project provides an HTTP "file" reader implementation of the [`IReportReader`](../Entities/IReportReader.cs) interface. It loads benchmark reports in JSON format from a webserver. The webserver can be configured using the `Readers.JsonFileReader.BaseURI` config setting (normally read from [appsettings.json](../Frontend/wwwroot/appsettings.json)), and otherwise defaults to the webserver the PrimeView app is loaded from. Which files it loads (or attempts to) depends on whether the `Readers.JsonFileReader.Index` config setting is specified: 
  * If it is, the specified index file (which should contain a JSON array of file names) is read first, after which the files included in that file  are retrieved. If an error occurs while reading or parsing the index, the application defaults to the behaviour mentioned in the next point. If reading the index is successful but an HTTP error occurs when reading one of the report files in the index list, the file in question is skipped, but reading continues.
  * If it is not, report files are read from the the `data` directory on the webserver. It starts with loading `report1.json`, then loads `report2.json`, and so on, until it receives an HTTP error on the request for a `report<number>.json` file.
* The [`ExtensionMethods`](ExtensionMethods.cs) class includes a `GetStableHashCode` methodes that provides what it says on the tin. This is used to make sure that hash codes of [`Report`](../Entities/Report.cs) JSON blobs loaded from the `report<number>.json` files remain the same across different builds of the PrimeView solution('s projects). This greatly simplifies testing if the [`Report.ID`](../Entities/Report.cs) field is derived from the report's JSON text, which is a fallback if it is not provided otherwise.