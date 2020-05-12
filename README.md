# Private DNS Functions

An Azure Function that responds to `Microsoft.Resources.ResourceWriteSuccess` and `Microsoft.Resources.ResourceDeleteSuccess` events for Private Endpoints and Network Interfaces to automatically add DNS recordsets in the appropriate Private DNS Zone.

This is useful for enabling automatic DNS registration at scale across any number of Subscriptions and across regions. Additionally, the ability to tag a NIC with a hostname streamlines the creation of DNS entries for resources such as internal load balancers or providing alternate hostnames for VMs.

# Setup & Usage

Coming Soon...

# Enhancements

1. Converting to Durable Azure Functions
1. Add outstanding Private Endpoint services.

# LICENSE

Copyright 2020 Ryan Graham

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.