﻿using StatisticsAnalysisTool.Common;

namespace StatisticsAnalysisTool.Models.TranslationModel;

public class DonationTranslation
{
    public static string HowCanIDonate => LanguageController.Translation("HOW_CAN_I_DONATE");
    public static string TopDonationsAllTime => LanguageController.Translation("TOP_DONATIONS_ALL_TIME");
    public static string TopDonationsThisMonth => LanguageController.Translation("TOP_DONATIONS_THIS_MONTH");
    public static string DonationDescription => LanguageController.Translation("DONATION_DESCRIPTION");
    public static string NoDonationsYet => LanguageController.Translation("NO_DONATION_YET");
}