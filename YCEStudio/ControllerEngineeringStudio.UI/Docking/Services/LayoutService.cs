using ControllerEngineeringStudio.Common.IO;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using Telerik.Windows.Controls;

namespace ControllerEngineeringStudio.UI.Docking
{
    /// <summary>
    /// レイアウトサービス
    /// </summary>
    internal class LayoutService : ILayoutService
    {
        #region Fields
        /// <summary>ドッキングマネージャー</summary>
        private readonly IDockingManager _dockingManager;

        /// <summary>ロガー</summary>
        private readonly ILogger<LayoutService> _logger;
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="dockingManager">ドッキングマネージャー</param>
        /// <param name="logger">ロガー</param>
        public LayoutService(IDockingManager dockingManager, ILogger<LayoutService> logger)
        {
            _dockingManager = dockingManager;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task LoadLayoutAsync(RadDocking radDocking)
        {
            string filePath = AppPath.DockingLayoutFile;
            if (!File.Exists(filePath))
            {
                // ファイルがない
                return;
            }

            // ドッキングアイテムを復元する
            await LoadDockingItemsAsync();

            // ドッキングアイテムの位置を復元する
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                radDocking.LoadLayout(stream);
                _logger.LogDebug("RadDockingレイアウト復元成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RadDockingレイアウト復元失敗");
            }
        }

        /// <summary>
        /// ドッキングアイテムの復元
        /// </summary>
        private async Task LoadDockingItemsAsync()
        {
            // RadPaneのSerializationTagのみ抽出し、ツール画面を表示する
            try
            {
                var doc = XDocument.Load(AppPath.DockingLayoutFile);
                var tags = doc.Descendants()
                    .Where(x => x.Name.LocalName.Equals(nameof(RadPane)))
                    .Select(x => x.Attribute(RadDocking.SerializationTagProperty.Name))
                    .Select(x => x?.Value)
                    .Where(x => x != null);
                foreach (var tag in tags)
                {
                    try
                    {
                        var parameter = JsonSerializer.Deserialize<DockingParameter>(tag!);
                        if (parameter != null)
                        {
                            await _dockingManager.OpenAsync(parameter);
                            _logger.LogDebug("DockingItems復元成功 SerializationTag:{SerializationTag}", tag);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "DockingItems復元失敗 SerializationTag:{SerializationTag}", tag);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DockingItems復元失敗");
            }
        }

        /// <inheritdoc/>
        public void SaveLayout(RadDocking radDocking)
        {
            // レイアウトの保存
            try
            {
                string filePath = AppPath.DockingLayoutFile;
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                using var stream = File.Create(filePath);
                radDocking.SaveLayout(stream);
                _logger.LogDebug("RadDockingレイアウト保存成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RadDockingレイアウト保存失敗");
            }
        }

        #region RadDocking Events
        /// <inheritdoc/>
        public void OnRadDockingElementLayoutSaving(object sender, LayoutSerializationSavingEventArgs e)
        {
            if (e.AffectedElement is RadDocumentPane)
            {
                // ドキュメントは保存しない
                e.Cancel = true;
            }
            else if (e.AffectedElement is RadPane pane
                && pane.IsHidden)
            {
                // 非表示になっているツール画面は保存しない
                e.Cancel = true;
            }
        }

        /// <inheritdoc/>
        public void OnRadDockingElementLoading(object sender, LayoutSerializationLoadingEventArgs e)
        {
            // RadPaneのうち、DockingItemsが復元されているものだけ復元する
            if (e.AffectedElement is not RadPane)
            {
                if (!string.IsNullOrEmpty(e.AffectedElementSerializationTag)
                    && e.AffectedElementSerializationTag.Contains(nameof(DockingParameter.DockingId)))
                {
                    e.Cancel = true;
                }
                return;
            }

            try
            {
                var parameter = JsonSerializer.Deserialize<DockingParameter>(e.AffectedElementSerializationTag);
                if (parameter == null)
                {
                    e.Cancel = true;
                }
                else if (!_dockingManager.IsOpened(parameter))
                {
                    e.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deserialize失敗 SerializationTag:{SerializationTag}",
                    e.AffectedElementSerializationTag);
                e.Cancel = true;
            }
        }
        #endregion
    }
}
