
###############################################
# LEGACY 
# Instead: have bunch of configuration files sitting in a well-known spot, discover them, feed them to AutoRest, done.

regenExpected = (opts,done) ->
  keys = Object.getOwnPropertyNames(opts.mappings)
  instances = keys.length

  for kkey in keys
    optsMappingsValue = opts.mappings[kkey]
    key = kkey.trim();
    
    swaggerFiles = (if optsMappingsValue instanceof Array then optsMappingsValue[0] else optsMappingsValue).split(";")
    args = [
      "--standalone-modeler",
      "--clear-output-folder",
      "--output-artifact=code-model-v1.yaml",
      "--output-folder=#{opts.outputDir}/#{key}"
    ]

    for swaggerFile in swaggerFiles
      args.push("--input-file=#{opts.inputBaseDir}/#{swaggerFile}")

    if (opts['override-info.version'])
      args.push("--override-info.version=#{opts['override-info.version']}")
    if (opts['override-info.title'])
      args.push("--override-info.title=#{opts['override-info.title']}")
    if (opts['override-info.description'])
      args.push("--override-info.description=#{opts['override-info.description']}")

    if (argv.args)
      for arg in argv.args.split(" ")
        args.push(arg);

    autorest args,() =>
      instances--
      return done() if instances is 0

mappingsTestServer = {
  'azure-parameter-grouping'    : 'azure-parameter-grouping.json',
  'azure-report'                : 'azure-report.json',
  'azure-resource-x'            : 'azure-resource-x.json',
  'azure-resource'              : 'azure-resource.json',
  'azure-special-properties'    : 'azure-special-properties.json',
  'body-array'                  : 'body-array.json',
  'body-boolean'                : 'body-boolean.json',
  'body-boolean.quirks'         : 'body-boolean.quirks.json',
  'body-byte'                   : 'body-byte.json',
  'body-complex'                : 'body-complex.json',
  'body-date'                   : 'body-date.json',
  'body-datetime-rfc1123'       : 'body-datetime-rfc1123.json',
  'body-datetime'               : 'body-datetime.json',
  'body-dictionary'             : 'body-dictionary.json',
  'body-duration'               : 'body-duration.json',
  'body-file'                   : 'body-file.json',
  'body-formdata-urlencoded'    : 'body-formdata-urlencoded.json',
  'body-formdata'               : 'body-formdata.json',
  'body-integer'                : 'body-integer.json',
  'body-number'                 : 'body-number.json',
  'body-number.quirks'          : 'body-number.quirks.json',
  'body-string'                 : 'body-string.json',
  'body-string.quirks'          : 'body-string.quirks.json',
  'complex-model'               : 'complex-model.json',
  'custom-baseUrl-more-options' : 'custom-baseUrl-more-options.json',
  'custom-baseUrl'              : 'custom-baseUrl.json',
  'extensible-enums-swagger'    : 'extensible-enums-swagger.json',
  'head-exceptions'             : 'head-exceptions.json',
  'head'                        : 'head.json',
  'header'                      : 'header.json',
  'httpInfrastructure'          : 'httpInfrastructure.json',
  'httpInfrastructure.quirks'   : 'httpInfrastructure.quirks.json',
  'lro'                         : 'lro.json',
  'model-flattening'            : 'model-flattening.json',
  'paging'                      : 'paging.json',
  'parameter-flattening'        : 'parameter-flattening.json',
  'report'                      : 'report.json',
  'required-optional'           : 'required-optional.json',
  'storage'                     : 'storage.json',
  'subscriptionId-apiVersion'   : 'subscriptionId-apiVersion.json',
  'url-multi-collectionFormat'  : 'url-multi-collectionFormat.json',
  'url'                         : 'url.json',
  'validation'                  : 'validation.json'
}

mappingsSpecs = {
  'specs-compute'          : 'compute/resource-manager/Microsoft.Compute/2017-03-30/compute.json',
  'specs-network'          : 'network/resource-manager/Microsoft.Network/2017-10-01/network.json',
  'specs-web'              : 'web/resource-manager/Microsoft.Web/2016-08-01/WebApps.json',
  'specs-mobileengagement' : 'mobileengagement/resource-manager/Microsoft.MobileEngagement/2014-12-01/mobile-engagement.json',
  'specs-datalake-store'   : 'datalake-store/data-plane/Microsoft.DataLakeStore/2016-11-01/filesystem.json',
  'specs-search'           : 'search/data-plane/Microsoft.Search/2016-09-01/searchindex.json',
  'specs-batch'            : 'batch/data-plane/Microsoft.Batch/2017-09-01.6.0/BatchService.json'
}

task 'regenerate-testserver', '', (done) ->
  regenExpected {
    'inputBaseDir': "node_modules/@microsoft.azure/autorest.testserver/swagger",
    'mappings': mappingsTestServer,
    'outputDir': 'test/Expected'
  },done
  return null

task 'regenerate-specs', '', (done) ->
  regenExpected {
    'inputBaseDir': "https://github.com/Azure/azure-rest-api-specs/blob/2df71489cc110ca9d3251bf7a4e685ab6616f379/specification",
    'mappings': mappingsSpecs,
    'outputDir': 'test/Expected'
  },done
  return null

task 'regenerate', "regenerate expected code for tests", ['regenerate-testserver', 'regenerate-specs'], (done) ->
  done();