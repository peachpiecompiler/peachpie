# Install .NET Core (see https://www.microsoft.com/net/core#ubuntu)

# Add the dotnet apt-get feed (for .NET Core)
sudo sh -c 'echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet-release/ trusty main" > /etc/apt/sources.list.d/dotnetdev.list'
sudo apt-key adv --keyserver apt-mo.trafficmanager.net --recv-keys 417A0893

# Update apt-get cache
sudo apt-get update

# Install .NET Core SDK
sudo apt-get install dotnet-dev-1.0.0-preview2-003131 -y


# Install Powershell (https://www.rootusers.com/how-to-install-powershell-on-linux)

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