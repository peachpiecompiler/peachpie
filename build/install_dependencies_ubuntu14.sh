# According to https://www.microsoft.com/net/core#ubuntu

# Add the dotnet apt-get feed
sudo sh -c 'echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet-release/ trusty main" > /etc/apt/sources.list.d/dotnetdev.list'
sudo apt-key adv --keyserver apt-mo.trafficmanager.net --recv-keys 417A0893

# Update apt-get cache
sudo apt-get update

# Install .NET Core SDK
sudo apt-get install dotnet-dev-1.0.0-preview2-003131