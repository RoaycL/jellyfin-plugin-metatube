using Jellyfin.Plugin.MetaTube.Helpers;
using Jellyfin.Plugin.MetaTube.Translation;
#if __EMBY__
using System.ComponentModel;
using Emby.Web.GenericEdit;
using MediaBrowser.Model.Attributes;

#else
using MediaBrowser.Model.Plugins;
#endif

namespace Jellyfin.Plugin.MetaTube.Configuration;

#if __EMBY__
public class PluginConfiguration : EditableOptionsBase
{
    public override string EditorTitle => "MetaTube 设置";
#else
public class PluginConfiguration : BasePluginConfiguration
{
#endif

#if __EMBY__
    [DisplayName("服务器")]
    [Description("MetaTube 服务端完整地址，建议使用 HTTPS。")]
    [Required]
#endif
    public string Server { get; set; } = string.Empty;

#if __EMBY__
    [DisplayName("令牌")]
    [Description("MetaTube 服务端访问令牌；如果后端未设置令牌可留空。")]
#endif
    public string Token { get; set; } = string.Empty;

#if __EMBY__
    [DisplayName("启用自动更新")]
    [Description("通过计划任务自动更新插件。")]
    public bool EnableAutoUpdate { get; set; } = true;
#endif

#if __EMBY__
    [DisplayName("启用合集")]
    [Description("按系列自动创建合集。")]
#endif
    public bool EnableCollections { get; set; } = false;

#if __EMBY__
    [DisplayName("启用导演")]
    [Description("将导演写入对应视频元数据。")]
#endif
    public bool EnableDirectors { get; set; } = true;

#if __EMBY__
    [DisplayName("启用评分")]
    [Description("显示来源站点的社区评分。")]
#endif
    public bool EnableRatings { get; set; } = true;

#if __EMBY__
    [DisplayName("启用预告片")]
    [Description("生成在线视频预告（strm 格式）。")]
#endif
    public bool EnableTrailers { get; set; } = false;

#if __EMBY__
    [DisplayName("启用演员真名")]
    [Description("从 AVBASE 获取并替换为演员真名。")]
#endif
    public bool EnableRealActorNames { get; set; } = false;

#if __EMBY__
    [DisplayName("启用角标")]
    [Description("为主图添加中文字幕角标。")]
#endif
    public bool EnableBadges { get; set; } = false;

#if __EMBY__
    [DisplayName("角标地址")]
    [Description("自定义角标地址，建议使用 PNG。（默认：zimu.png）")]
#endif
    public string BadgeUrl { get; set; } = "zimu.png";

#if __EMBY__
    [DisplayName("主图比例")]
    [Description("主图宽高比，设为负数时使用默认值。")]
#endif
    public double PrimaryImageRatio { get; set; } = -1;

#if __EMBY__
    [DisplayName("默认图片质量")]
    [Description("JPEG 图片默认压缩质量，范围 0 到 100。（默认：90）")]
    [MinValue(0)]
    [MaxValue(100)]
    [Required]
#endif
    public int DefaultImageQuality { get; set; } = 90;

#if __EMBY__
    [DisplayName("启用影片来源过滤")]
    [Description("过滤并重排影片来源的搜索结果。")]
#endif
    public bool EnableMovieProviderFilter { get; set; } = false;

#if __EMBY__
    [DisplayName("影片来源过滤")]
    [Description(
        "来源名称不区分大小写，按从左到右优先级递减，使用逗号分隔。")]
#endif
    public string RawMovieProviderFilter
    {
        get => _movieProviderFilter?.Any() == true ? string.Join(',', _movieProviderFilter) : string.Empty;
        set => _movieProviderFilter = value?.Split(',').Select(s => s.Trim()).Where(s => s.Any())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public List<string> GetMovieProviderFilter()
    {
        return _movieProviderFilter;
    }

    private List<string> _movieProviderFilter;

#if __EMBY__
    [DisplayName("启用模板")]
#endif
    public bool EnableTemplate { get; set; } = false;

#if __EMBY__
    [DisplayName("名称模板")]
#endif
    public string NameTemplate { get; set; } = DefaultNameTemplate;

#if __EMBY__
    [DisplayName("副标题模板")]
#endif
    public string TaglineTemplate { get; set; } = DefaultTaglineTemplate;

    public static string DefaultNameTemplate => "{number} {title}";

    public static string DefaultTaglineTemplate => "配信開始日 {date}";

#if __EMBY__
    [DisplayName("翻译模式")]
#endif
    public TranslationMode TranslationMode { get; set; } = TranslationMode.Disabled;

#if __EMBY__
    [DisplayName("翻译引擎")]
#endif
    public TranslationEngine TranslationEngine { get; set; } = TranslationEngine.Baidu;

#if __EMBY__
    [DisplayName("百度 App ID")]
    [VisibleCondition(nameof(TranslationEngine), ValueCondition.IsEqual, TranslationEngine.Baidu)]
#endif
    public string BaiduAppId { get; set; } = string.Empty;

#if __EMBY__
    [DisplayName("百度 App Key")]
    [VisibleCondition(nameof(TranslationEngine), ValueCondition.IsEqual, TranslationEngine.Baidu)]
#endif
    public string BaiduAppKey { get; set; } = string.Empty;

#if __EMBY__
    [DisplayName("Google API 密钥")]
    [VisibleCondition(nameof(TranslationEngine), ValueCondition.IsEqual, TranslationEngine.Google)]
#endif
    public string GoogleApiKey { get; set; } = string.Empty;

#if __EMBY__
    [DisplayName("Google API 地址")]
    [Description("自定义 Google 翻译 API 地址。（可选）")]
    [VisibleCondition(nameof(TranslationEngine), ValueCondition.IsEqual, TranslationEngine.Google)]
#endif
    public string GoogleApiUrl { get; set; } = string.Empty;

#if __EMBY__
    [DisplayName("DeepL API 密钥")]
    [VisibleCondition(nameof(TranslationEngine), ValueCondition.IsEqual, TranslationEngine.DeepL)]
#endif
    public string DeepLApiKey { get; set; } = string.Empty;

#if __EMBY__
    [DisplayName("DeepL API 地址")]
    [Description("自定义兼容 DeepL 的 API 地址。（可选）")]
    [VisibleCondition(nameof(TranslationEngine), ValueCondition.IsEqual, TranslationEngine.DeepL)]
#endif
    public string DeepLApiUrl { get; set; } = string.Empty;

#if __EMBY__
    [DisplayName("OpenAI API 密钥")]
    [VisibleCondition(nameof(TranslationEngine), ValueCondition.IsEqual, TranslationEngine.OpenAi)]
#endif
    public string OpenAiApiKey { get; set; } = string.Empty;

#if __EMBY__
    [DisplayName("OpenAI API 地址")]
    [Description("自定义兼容 OpenAI 的 API 地址。（可选）")]
    [VisibleCondition(nameof(TranslationEngine), ValueCondition.IsEqual, TranslationEngine.OpenAi)]
#endif
    public string OpenAiApiUrl { get; set; } = string.Empty;

#if __EMBY__
    [DisplayName("OpenAI 模型")]
    [Description("自定义兼容 OpenAI 的模型名。（可选）")]
    [VisibleCondition(nameof(TranslationEngine), ValueCondition.IsEqual, TranslationEngine.OpenAi)]
#endif
    public string OpenAiModel { get; set; } = string.Empty;

#if __EMBY__
    [DisplayName("启用标题替换")]
#endif
    public bool EnableTitleSubstitution { get; set; } = false;

#if __EMBY__
    [DisplayName("标题替换表")]
    [Description(
        "每行一条记录，使用等号分隔。目标文本留空表示删除源文本。")]
    [EditMultiline(5)]
#endif
    public string TitleRawSubstitutionTable
    {
        get => _titleSubstitutionTable?.ToString();
        set => _titleSubstitutionTable = SubstitutionTable.Parse(value);
    }

    public SubstitutionTable GetTitleSubstitutionTable()
    {
        return _titleSubstitutionTable;
    }

    private SubstitutionTable _titleSubstitutionTable;

#if __EMBY__
    [DisplayName("启用演员替换")]
#endif
    public bool EnableActorSubstitution { get; set; } = false;

#if __EMBY__
    [DisplayName("演员替换表")]
    [Description(
        "每行一条记录，使用等号分隔。目标演员留空表示删除源演员。")]
    [EditMultiline(5)]
#endif
    public string ActorRawSubstitutionTable
    {
        get => _actorSubstitutionTable?.ToString();
        set => _actorSubstitutionTable = SubstitutionTable.Parse(value);
    }

    public SubstitutionTable GetActorSubstitutionTable()
    {
        return _actorSubstitutionTable;
    }

    private SubstitutionTable _actorSubstitutionTable;

#if __EMBY__
    [DisplayName("启用类型替换")]
#endif
    public bool EnableGenreSubstitution { get; set; } = false;

#if __EMBY__
    [DisplayName("类型替换表")]
    [Description(
        "每行一条记录，使用等号分隔。目标类型留空表示删除源类型。")]
    [EditMultiline(5)]
#endif
    public string GenreRawSubstitutionTable
    {
        get => _genreSubstitutionTable?.ToString();
        set => _genreSubstitutionTable = SubstitutionTable.Parse(value);
    }

    public SubstitutionTable GetGenreSubstitutionTable()
    {
        return _genreSubstitutionTable;
    }

    private SubstitutionTable _genreSubstitutionTable;
}