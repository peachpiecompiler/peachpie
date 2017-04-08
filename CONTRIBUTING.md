# Contribution Guidelines

## Prerequisites 
By contributing to the Peachpie Compiler Platform, you state that:

* The contribution is your own original work and does not infringe any copyrights.
* Your work is not owned by your employer (or you have been given copyright assignment in writing) and you are therefore allowed to assign the copyrights to your contribution to Peachpie.
* You [license](https://github.com/iolevel/peachpie/blob/master/LICENSE.txt) the contribution under the terms applied to the rest of the Peachpie project.

## Coding Standards
### Code style

For the Peachpie project (excluding files written in PHP), the standard .NET coding guidelines apply.
Please refer to the [Framework Design Guidelines](https://msdn.microsoft.com/en-us/library/ms229042%28v=vs.110%29.aspx) for more information.

### Unit tests

Please run all unit tests prior to creating a PR. All pull requests that did not pass the automated CI testing will be rejected.

## Contributing to Peachpie
Before you make a commit, please ensure that it meets the following requirements:

 * Your commit is a small logical unit that represents a reasonable change.
 * You should include new or changed tests relevant to the changes you are making.
 * Please avoid unnecessary whitespace. Check for whitespace with `git diff --check` and `git diff --cached --check` before commiting.
 * The code included in your commit should compile without errors or warnings.
 * All tests should be passing.
 * A reasonable amount of comments is included in order for the code to be transparent for all users.

### Submit your PR
Once you believe your commit meets the above requirements, feel free to submit your pull request. Kindly ensure you have met the following guidelines:
* In the pull request, summarize the contents of your commit or issues that you are resolving.
* Once the pull request is in, please do not delete the branch or close the pull request (unless something is wrong with it).
* We will respond to your pull request in a reasonable timeframe. Should there be a reason for us to reject your PR, we will let you know in the comments.

