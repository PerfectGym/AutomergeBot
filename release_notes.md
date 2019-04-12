## RELEASE NOTES

### VERSION 1.3.0

**Features**
- Assign an additional random reviewer to pull request after approval

### VERSION 1.2.4
- Extracted hardcoded NotifyUserAboutPullRequestWithUnresolvedConflicts message template to text file.

### VERSION 1.2.3

**Features**
- Possible to configure mapping from github user's email to Slack nick.
  These mappings are used by SlackClient to put correct direct mentions inside notifications.
- Order not merged PR's notifications on the Slack from the newest to the oldest
- Simplified steps in the message in case of conflicts - 2 command lines  changed to 1 command line. 
  It makes impoosible for the human to mistakenly execute 2nd command line step if the former failed.
  

### VERSION 1.2.2

**Features**
- Add to Slack notificaiton messages information how long given PR is open (diff between now and PR creation time)
- Present all info in a single Slack message per user. Do not repeat formula: "please resolve conflicts manually, approve review and merge pull request"
- Updated Instruction when conflicts occured. 
If somebody's PR was not merged before ours then event after merging that PR the ours still has information about conflicting files. This information is outdated but GitHub does not updates it.

### VERSION 1.0.0

**Features**
- automatic merging of branches according to provided merge directions
- automatic creation of temporary branches and pull requests in case merging has to be done manually
- notification service for informing people about need of manual merging
- automatic reloading of merge directions when changed - no need to stop the app in order for changes to take place
- preserving original author of changes in newly created pull requests


### VERSION 1.2.0

**Features**
- Isolated critical functionality from non-critical. If non-critical fails it does not block critical successful execution.  
  Critical is to create PR for newly pushed changes and merge it. Whereas non-critical is removing temp branches, retrying merge PR etc.
- Better structure of the source code in the spirit of SRP.
- Logging details for all errors returned by calls to GitHub API


**Bugfixes**
- Removing temporary branches which should not be removed (causes closing PR which is not merged).
  When merging from A->B and A->C closing PR[A->B] caused removing temporary branch of PR[A->C]. It was because they both have temporary branches with the same commit.
  Now only closing PR removes its temporary branch.

### VERSION 1.1.0

**Features**
- automatic removal of all temporary branches which are no longer needed  
  Upon closing pull request all temporary branches which are no longer needed are deleted from the remote repository.
- slack notifications about conflicts waiting for resolve  
  Notification service integrated with Slack for pinging people who do not merge their pull requests for a long time.
- automatic retry merge pull requests   
  Upon pushing new code to one of the monitored branches, automatic attempt to merge all pull requests targeting this branch, based on hope that newly added code resolved existing conflicts also for them.

**Bugfixes**
- reloading merging directions from configuration without restarting service
- missing details for errors received from GitHub







