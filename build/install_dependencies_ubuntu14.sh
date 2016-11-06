# Install .NET Core (see https://www.microsoft.com/net/core#ubuntu),
# Mono (http://www.mono-project.com/docs/getting-started/install/linux)
# and Powershell (https://www.rootusers.com/how-to-install-powershell-on-linux)

# Add the dotnet apt-get feed (for .NET Core)
sudo sh -c 'echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet-release/ trusty main" > /etc/apt/sources.list.d/dotnetdev.list'
sudo apt-key adv --keyserver apt-mo.trafficmanager.net --recv-keys 417A0893

# Add the Mono apt-get feed
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list

# Update apt-get cache
sudo apt-get update

# Install .NET Core SDK
sudo apt-get install dotnet-dev-1.0.0-preview2-003131 -y

# Install Mono
sudo apt-get install mono-complete -y

# Install Powershell dependencies
sudo apt-get install libunwind8 libicu52 -y

# Download and install Powershell
wget https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.11/powershell_6.0.0-alpha.11-1ubuntu1.14.04.1_amd64.deb
sudo dpkg -i powershell_6.0.0-alpha.11-1ubuntu1.14.04.1_amd64.deb
rm -f powershell_6.0.0-alpha.11-1ubuntu1.14.04.1_amd64.deb


# Install Python Pip and cdiff

sudo apt-get -y install python-pip
pip -V
pip install --upgrade cdiff