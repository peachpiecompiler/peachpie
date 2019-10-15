# Install .NET Core # https://www.microsoft.com/net/download/linux-package-manager/ubuntu14-04/sdk-current

# Register Microsoft key and feed
#wget -q packages-microsoft-prod.deb https://packages.microsoft.com/config/ubuntu/14.04/packages-microsoft-prod.deb
#sudo dpkg -i packages-microsoft-prod.deb

sudo apt-get install -y gpg
wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg
sudo mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
wget -q https://packages.microsoft.com/config/ubuntu/16.04/prod.list
sudo mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
sudo chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg
sudo chown root:root /etc/apt/sources.list.d/microsoft-prod.list

# Install .NET Core SDK
sudo apt-get update
sudo apt-get install apt-transport-https
sudo apt-get update
sudo apt-get install dotnet-sdk-3.0 -y

# Install Powershell (https://www.rootusers.com/how-to-install-powershell-on-linux)

# Install Powershell dependencies
sudo apt-get install libunwind8 libicu52 liblttng-ust0 -y

# Download and install Powershell
wget https://github.com/PowerShell/PowerShell/releases/download/v6.1.6/powershell_6.1.6-1.ubuntu.14.04_amd64.deb
sudo dpkg -i powershell_6.1.6-1.ubuntu.14.04_amd64.deb
rm -f powershell_6.1.6-1.ubuntu.14.04_amd64.deb

# Install Python Pip and icdiff (http://www.jefftk.com/icdiff)

sudo apt-get -y install python-pip
pip -V
sudo pip install --upgrade icdiff
