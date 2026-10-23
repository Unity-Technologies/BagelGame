using NUnit.Framework;

namespace Bagel
{
    // Every test plays in English and never touches the player's save; both are put back afterwards.
    [SetUpFixture]
    public class BagelTestsSetUp
    {
        string m_LocaleCode;
        bool m_SaveWasEnabled;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_LocaleCode = BagelLocalization.localeCode;
            BagelLocalization.SetLocale("en");

            m_SaveWasEnabled = BagelSave.enabled;
            BagelSave.enabled = false;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            BagelLocalization.SetLocale(m_LocaleCode);
            BagelSave.enabled = m_SaveWasEnabled;
        }
    }
}
