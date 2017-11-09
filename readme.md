# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.


# AutoRest extension configuration

## Run it as a standalone (good for testing)

``` yaml $(standalone-modeler)
pipeline:
  standalone/imodeler1:
    input: openapi-document/identity
    output-artifact: code-model-v1
    scope: standalone-modeler
  standalone/commonmarker:
    input: imodeler1
    output-artifact: code-model-v1
  standalone/cm/transform:
    input: commonmarker
    output-artifact: code-model-v1
  standalone/cm/emitter:
    input: transform
    scope: scope-cm/emitter
```

## Run it before generator

``` yaml
# pipeline configuration is part of generators which make use of this extension (modeler must run under generator's scope)

# used by generators:
scope-transform-string:
  is-object: false
```
