# Install .NET Core # https://www.microsoft.com/net/download/linux-package-manager/ubuntu14-04/sdk-current

# Register Microsoft key and feed
wget -q https://packages.microsoft.com/config/ubuntu/14.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

# Install .NET Core SDK
sudo apt-get install apt-transport-https
sudo apt-get update
sudo apt-get install dotnet-sdk-2.1 -y

# Install Powershell (https://www.rootusers.com/how-to-install-powershell-on-linux)

# Install Powershell dependencies
sudo apt-get install libunwind8 libicu52 -y

# Download and install Powershell
wget https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.11/powershell_6.0.0-alpha.11-1ubuntu1.14.04.1_amd64.deb
sudo dpkg -i powershell_6.0.0-alpha.11-1ubuntu1.14.04.1_amd64.deb
rm -f powershell_6.0.0-alpha.11-1ubuntu1.14.04.1_amd64.deb


# Install Python Pip and icdiff (http://www.jefftk.com/icdiff)

sudo apt-get -y install python-pip
pip -V
sudo pip install --upgrade icdiff
