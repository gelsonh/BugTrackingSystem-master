using BugTrackingSystem.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BugTrackingSystem.Controllers
{
    [Controller]
    public abstract class BTBaseController : Controller
    {
        protected int _companyId => User.Identity!.GetCompanyId();
    }
}
