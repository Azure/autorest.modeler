﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace AutoRest.Modeler.Model
{
    /// <summary>
    /// The location of the parameter.
    /// </summary>
    public enum ParameterLocation
    {
        None,
        Query,
        Header,
        Path,
        FormData,
        Body,
        Cookie
    }
}
