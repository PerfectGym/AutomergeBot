# AdditionalCodeReview
This feature allows assigning a random additional reviewer after approved.


## Configuration

The file appsetting.json contains configuration for the feature

Example:

```
"AdditionalCodeReviewConfiguration":{
		"AdditionalReviewProbability":"30", //0-100 
		"Reviewers": ["pawelmieten","owerkop"],
		"NeedAdditionalReviewLabel":"AdditionalReview", //Label name for PR where is need additional review
		"NoNeedAdditionalReviewLabel":"NoAdditionalReview" //Label name for PR where is NOT need additional review
	}

```

**AdditionalReviewProbability**  - indicate chance to assign an additional reviewer eg 30 mean 30% chance. 
To turn off the feature you should set 0. There is allowed a value of range 0-100.

**Reviewers** - The list of potential additional reviewers.

**NeedAdditionalReviewLabel**  OR **NeedAdditionalReviewLabel**  - Names of labels to mark a pull request in case of select additional reviewer or not.

