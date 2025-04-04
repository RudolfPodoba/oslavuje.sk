using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Dashboards;

namespace oslavuje.sk.Composers;

public class DashboardsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Dashboards().Remove<ContentDashboard>();
        //builder.TheDashboardCounters().Remove<MembersTotalDashboardCounter>()
        //    .Remove<MembersNewLastWeekDashboardCounter>();
    }
}