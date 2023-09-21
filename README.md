# MasterNode Setup Tool

Welcome!

The Masternode Setup Tool is a seamless application tailored to make your integration into the federation smoother than ever.

> This project is the next step of the evolution of [Masternode Launch Script](https://academy.stratisplatform.com/Operation%20Guides/InterFlux%20Masternodes/Running%20Masternode/running-the-masternode.html), which is deprecated now.

Features:

 * Easy-to-Use Interface: Dive right in with an intuitive design, catering to both beginners and experienced users.
 * Private Key Generation: Securely create private keys essential for participating within the federation, ensuring utmost security and compliance.
 * One-Click Requirement Installation: Say goodbye to manual setups. Our tool ensures all prerequisites are automatically installed, ensuring a hassle-free setup.
 * Wallet Management:
   - Creation: Set up new wallets effortlessly to join the federation.
   - Recovery: Misplaced or lost your previous wallet? No worries. Our recovery feature will help you get back on track.
   - Existing Wallet Integration: Have an existing wallet? Easily integrate it with our tool and join the federation in no time.

## Get started

### Setting up with installer

1. To find the installer package, please check out the [Releases page](https://github.com/stratisproject/MasternodeSetupTool/releases)
2. Install the application with .msi installer file
3. Run the application and follow the instructions

### Building from sources

1. Install VS2022 and .NET SDK 6
2. Clone repository, including [submodules](https://git-scm.com/book/en/v2/Git-Tools-Submodules)
3. Build MasternodeSetupToolInstaller, and install the application using the created .msi file.

## How it works 

![Algorithm scheme](Documentation/algorithm.svg)

## Dependencies

This repository depends on two projects:
- https://github.com/stratisproject/StratisFullNode.git
- https://github.com/stratisproject/StratisMasternodeDashboard

## Additional links

![Changelog](https://github.com/stratisproject/MasternodeSetupTool/blob/master/CHANGELOG.md)

![Security Policy](https://github.com/stratisproject/MasternodeSetupTool/blob/master/SECURITY.md)

![Credits](https://github.com/stratisproject/MasternodeSetupTool/blob/master/CREDITS.md)


## [License](LICENSE) 

> MIT License
>
> Copyright (c) 2023 StratisPlatform
>
> Permission is hereby granted, free of charge, to any person obtaining a copy
> of this software and associated documentation files (the "Software"), to deal
> in the Software without restriction, including without limitation the rights
> to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
> copies of the Software, and to permit persons to whom the Software is
> furnished to do so, subject to the following conditions:
>
> The above copyright notice and this permission notice shall be included in all
> copies or substantial portions of the Software.
>
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
> IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
> FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
> AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
> LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
> OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
> SOFTWARE.
