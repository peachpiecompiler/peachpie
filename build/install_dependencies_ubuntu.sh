# pdo_sqlite
#pecl install pdo_sqlite

# Install .NET Core
# https://dotnet.microsoft.com/download/linux-package-manager/ubuntu16-04/sdk-current

# Register Microsoft key and feed
wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

# Install .NET Core SDK
sudo apt-get update
sudo apt-get install apt-transport-https
sudo apt-get update
sudo apt-get install dotnet-sdk-3.0 -y

# Install Python Pip and icdiff (http://www.jefftk.com/icdiff)

sudo apt-get -y install python-pip
pip -V
sudo pip install --upgrade icdiff
