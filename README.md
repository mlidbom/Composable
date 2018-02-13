# Composable

[Project site](http://composabletk.net/)

[![Gitter](https://badges.gitter.im/Composable4/Lobby.svg)](https://gitter.im/Composable4/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[Skype Chat](https://join.skype.com/awyeJlk3rVbu)


## Set up development environment
* Just open Composable.Everything.sln in Visual Studio 2017 and you should be good to go.
* To run the tests you need administator access to a SQL server installation. If you don't have one the development edition is free to download and use.

### Running the sample project
* The connectionstring AccountManagement in AccountManagement.Server/App.config must be valid.
 * The configured user must have full permissions to create tables etc in the database in the connection string.

### Environment varibles you should know about when running the tests

**COMPOSABLE_DATABASE_POOL_MASTER_CONNECTIONSTRING**: Let's you override the connection string to use for the database pool.

**COMPOSABLE_MACHINE_SLOWNESS**: 
Lets you adjust the expectations for the performance tests.  
For example: If you set it to 2.0 performance tests are allowed to take 2.0 times as long to complete without failing.

**COMPOSABLE_TEMP_DRIVE**:
Let's you move where temp data is stored out of the default system temp folder. 
Among other things the databases in the database pool are stored here.
If possible set this to a path on a fast SSD or, even better, a RAM drive.

## Set up documentation development environment

* Download and install python 2 from https://www.python.org/
  * Select option to add python to your path
* Install Ruby 2.2.5 from https://rubyinstaller.org/downloads/archives/
  * Check options
    * Add ruby executables to your path
    * Associate .rb and .rbw files with this ruby installation
  * Otherwise use all the default options all through the wizard.
* Download the 32 bit devkit for this version of ruby from: https://rubyinstaller.org/downloads/
  * Extract it to: C:\RubyDevKit\
  * Open a command prompt in C:\RubyDevKit\
  * ruby dk.rb init
  * ruby dk.rb install
* Follow instructions for fixing ssl cert issue here: http://guides.rubygems.org/ssl-certificate-update/#manual-solution-to-ssl-issue
* Open a prompt in folder: src\framework\Documentation\
  * gem install bundler
  * bundle install
  * install-dependencies.bat
  * build.bat
  * serve.bat
    * Open the url mentioned in the output
    * Edit the files in the project and happily watch the browser auto-update with your changes :)

