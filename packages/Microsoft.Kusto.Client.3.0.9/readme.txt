Kusto Client Library
====================

Updated documentation can be found at:
https://kusto.azurewebsites.net/docs/api/using_the_kusto_client_library.html


What's new?
===========
Version 3.0.9 (08 NOV 2017)
* Add support for EntityNotFoundException.

Version 3.0.8 (01 OCT 2017)
* Support explicit authority ID for AAD user authentication scenario.

Version 3.0.7 (13 SEP 2017)
* Fix hang when issuing multiple async requests requiring AAD authentication.

Version 3.0.6 (12 SEP 2017):
* Fix ADAL reference to version 3.16.1.

Version 3.0.5 (10 SEP 2017):
* Upgrade ADAL's version from 3.12.0 to 3.16.1

Version 3.0.4 (06 SEP 2017):
* Support Windows Hello for dSTS-based authentication (for Microsoft internal principals)

Version 3.0.3 (29 AUG 2017):
* CslCommandGenerator.GenerateExtentsDropByTagsCommand(): update to use a more efficient version of ExtentsShowCommand.

Version 3.0.2 (08 AUG 2017):
* Bug fix: Update Newtonsoft.Json dependency to version 10.0.3.

Version 3.0.1 (17 JULY 2017):
* Bug fix: Race condition in TraceSourceBase<> static construction.

Version 3.0.0 (05 JULY 2017):
* Upgrade Newtonsoft.Json to version 10.0.3 and Microsoft.WindowsAzure.Storage to version 8.1.4
* The client returns json errors instead of text errors.
* Client side is streaming.
* Dynamic values are encoded as json values and not strings.
* Boolean values may now be returned as Booleans instead of numbers.
* Strongly-typed clients based on ObjectReader return JSON.NET objects.

Version 2.5.12 (28 JUNE 2017):
* Bug fix - NullReference in RestClient2

Version 2.5.11 (28 JUNE 2017):
* Bug fix - using ClientRequestProperties.Application/UserName should override KustoConnectionStringBuilder.ApplicationNameForTracing/UserNameForTracing.

Version 2.5.10 (20 JUNE 2017):
* Bug fix - fix hang when running inside 'Orleans' framework

Version 2.5.9 (15 JUNE 2017):
* NetworkCache: Added support for setting timer start refreshing time and cache refreshing timeout.

Version 2.5.8 (08 JUNE 2017):
* Kusto.Cloud.Platform.Data.ObjectReader<T>: Added support for fields of type JToken (or derived).

Version 2.5.7 (22 MAY 2017):
* KCSB - block sending corporate credentials when using basic authentication

Version 2.5.6 (21 MAY 2017):
* Bug fix in ExecuteStreamIngestAsync - fix behavior of mappingName parameter

Version 2.5.5 (18 MAY 2017):
* Bug fix (null ref when runnin in Mono).

Version 2.5.4 (7 MAY 2017):
* Extend kusto ingestion error codes with 'NoError'.

Version 2.5.3 (27 APR 2017):
* Add kusto ingestion error codes.

Version 2.5.2 (09 APR 2017):
* Bug fix - support AAD token acquisition based-on application client ID and certificate thumbprint.

Version 2.5.1 (30 MAR 2017):
* Add Kusto Connection String validation.

Version 2.5.0 (16 MAR 2017):
* Target client library to .net 4.5 to enable customers that cannot use higher versions to use Kusto client.

Version 2.4.9 (13 FEB 2017):
* Support AAD Multi-Tenant access to Kusto for applications.

Version 2.4.8 (12 FEB 2017):
* Support AAD Multi-Tenant access to Kusto.

Version 2.4.7 (31 JAN 2017):
* Kusto clients version alignment.

Version 2.4.6 (24 DEC 2016):
* Fixing a bug in CslCommandGenerator's GenerateTableExtentsShowCommand().

Version 2.4.5 (24 NOV 2016):
* Extend Azure Storage retry policy in order to handle IO exceptions.

Version 2.4.4 (16 NOV 2016):
* Extend Azure Storage retry policy in order to handle web and socket exceptions.

Version 2.4.3 (16 NOV 2016):
* Support Multi-Factor Authentication enforcement for AAD-based authentication.

Version 2.4.2 (22 SEP 2016):
* Fix potential deadlock in 'ExecuteQuery' when running in IIS.

Version 2.4.1 (20 SEP 2016):
* Fix potential deadlock during AAD token acquisition.

Version 2.4.0 (19 SEP 2016):
* Security bug fix (client credentials leak to traces).

Version 2.3.9 (5 SEP 2016):
* Support dSTS-based application authentication.

Version 2.3.8 (12 AUG 2016):
* Target client library to .net 4.5 to enable customers that cannot use higher versions to use ksuto client.

Version 2.3.7 (12 AUG 2016):
* Fix issue where null pointer exceptions are thrown for client on syntax errors rather than a meaningful error
* Make ExecuteQueryAsync async all the way down incl. 3rd party libraries (ADAL).

Version 2.3.5 (24 JUL 2016):
* Fix UI potential deadlock during AAD token acquisition.

Version 2.3.4 (20 JUL 2016):
* Upgrade ADAL's version from 2.14.2011511115 to 3.12.0

Version 2.3.3 (19 JUL 2016):
* Supporting dSTS-based authentication for Microsoft internal principals. More details can be found at https://kusto.azurewebsites.net/docs/concepts/concepts_security_authn_dsts.html.