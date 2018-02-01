# Composable

[Project site](http://composabletk.net/)

[![Gitter](https://badges.gitter.im/Composable4/Lobby.svg)](https://gitter.im/Composable4/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[Skype Chat](https://join.skype.com/awyeJlk3rVbu)


### Environment varibles you should now about when running the tests

**COMPOSABLE_DATABASE_POOL_MASTER_CONNECTIONSTRING**: Let's you override the connection string to use for the database pool.

**COMPOSABLE_MACHINE_SLOWNESS**: 
Lets you adjust the expectations for the performance tests.  
For example: If you set it to 2.0 performance tests are allowed to take 2.0 times as long to complete without failing.

**COMPOSABLE_TEMP_DRIVE**:
Let's you move where temp data is stored out of the default system temp folder. 
Among other things the database in the database pool are store here.
If possible set this to a path on a fast SSD or, even better, a ram drive.