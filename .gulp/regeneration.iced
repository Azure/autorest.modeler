
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
      args.push("--input-file=#{if !!opts.inputBaseDir then "#{opts.inputBaseDir}/#{swaggerFile}" else swaggerFile}")

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

mappings = {
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

swaggerDir = "node_modules/@microsoft.azure/autorest.testserver/swagger"

task 'regenerate', '', (done) ->
  mappings['AcceptanceTests/AzureResource'] = 'azure-resource-x.json'
  regenExpected {
    'inputBaseDir': swaggerDir,
    'mappings': mappings,
    'outputDir': 'test/Expected'
  },done
  return null