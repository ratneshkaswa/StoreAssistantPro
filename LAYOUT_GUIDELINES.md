# StoreAssistantPro — WPF Layout Guidelines

> Standard layout rules for every View and Window in the application.
> Follow these patterns to prevent control cropping, excessive empty space,
> and inconsistent scaling across DPI configurations.

---

## Core Principles

| # | Rule | Rationale |
|---|---|---|
| 1 | **`Grid` is the primary layout container** | Provides row/column star sizing for proportional scaling |
| 2 | **Use star (`*`) sizing for expandable regions** | Content fills available space without hard-coded dimensions |
| 3 | **Use `Auto` only for fixed-height chrome** | Headers, toolbars, status bars, and action rows that size to content |
| 4 | **Avoid fixed `Width`/`Height` on containers** | Fixed values clip content on high-DPI or small screens |
| 5 | **Set `MinWidth`/`MinHeight` on critical controls** | Prevents collapse to zero when star-sized space is constrained |
| 6 | **`TextWrapping="Wrap"` on all user-facing text** | Prevents truncation when text exceeds container width |
| 7 | **Let `BaseDialogWindow` and `WindowSizingService` control window dimensions** | Never set `Width`, `Height`, or `ResizeMode` in XAML — see `Core/Base/BaseDialogWindow.cs` |

---

## Row/Column Sizing Reference

```
Auto   → Row/column shrinks to fit content.  Use for headers, labels, buttons.
*      → Row/column shares remaining space.  Use for scrollable/data areas.
2*     → Weighted star: gets twice the share of a 1* sibling.
```

### ✅ Correct — data area fills remaining space

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>   <!-- Page header -->
    <RowDefinition Height="Auto"/>   <!-- Toolbar / filters -->
    <RowDefinition Height="*"/>      <!-- DataGrid / ScrollViewer -->
    <RowDefinition Height="Auto"/>   <!-- Status / action bar -->
</Grid.RowDefinitions>
```

### ❌ Wrong — every row is `Auto`, content may overflow or leave dead space

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
</Grid.RowDefinitions>
```

---

## Standard Page Layout (UserControl hosted in MainWindow)

This is the template for any new navigable page registered via `NavigationPageRegistry`.

```xml
<UserControl x:Class="StoreAssistantPro.Modules.{Module}.Views.{Name}View"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d"
             d:DesignHeight="550" d:DesignWidth="800">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <!-- Row 0: Page title — fixed height, sizes to text -->
            <RowDefinition Height="Auto"/>
            <!-- Row 1: Toolbar / filters — fixed, sizes to buttons -->
            <RowDefinition Height="Auto"/>
            <!-- Row 2: Inline form (collapsible) — fixed when visible -->
            <RowDefinition Height="Auto"/>
            <!-- Row 3: Validation messages — fixed -->
            <RowDefinition Height="Auto"/>
            <!-- Row 4: Primary data area — STAR, fills remaining space -->
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- ── Row 0: Page Header ── -->
        <DockPanel Grid.Row="0" Margin="0,0,0,10">
            <TextBlock Text="Page Title"
                       FontSize="24" FontWeight="Bold"
                       DockPanel.Dock="Left"/>
            <Button Content="🔄 Refresh"
                    Command="{Binding RefreshCommand}"
                    HorizontalAlignment="Right"
                    Padding="12,6" FontSize="13"/>
        </DockPanel>

        <!-- ── Row 1: Toolbar ── -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,10">
            <Button Content="➕ Add" Command="{Binding AddCommand}"
                    Padding="10,5" Margin="0,0,5,0"/>
            <Button Content="✏️ Edit" Command="{Binding EditCommand}"
                    Padding="10,5" Margin="0,0,5,0"/>
            <Button Content="🗑 Delete" Command="{Binding DeleteCommand}"
                    Padding="10,5"/>
        </StackPanel>

        <!-- ── Row 2: Collapsible inline form ── -->
        <Border Grid.Row="2" Background="#F5F5F5"
                CornerRadius="6" Padding="15" Margin="0,0,0,10"
                Visibility="{Binding IsFormVisible,
                    Converter={StaticResource BoolToVisibility}}">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <StackPanel Grid.Column="0" Margin="0,0,15,0">
                    <TextBlock Text="Field One" FontSize="12" Margin="0,0,0,3"/>
                    <TextBox Text="{Binding FieldOne, UpdateSourceTrigger=PropertyChanged}"
                             Padding="5" MinWidth="120"/>
                </StackPanel>

                <StackPanel Grid.Column="1" Margin="0,0,15,0">
                    <TextBlock Text="Field Two" FontSize="12" Margin="0,0,0,3"/>
                    <TextBox Text="{Binding FieldTwo, UpdateSourceTrigger=PropertyChanged}"
                             Padding="5" MinWidth="80"/>
                </StackPanel>

                <StackPanel Grid.Column="2" VerticalAlignment="Bottom"
                            Orientation="Horizontal">
                    <Button Content="💾 Save" Command="{Binding SaveCommand}"
                            Padding="10,5" Margin="0,0,5,0"/>
                    <Button Content="Cancel" Command="{Binding CancelCommand}"
                            Padding="10,5"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- ── Row 3: Validation ── -->
        <TextBlock Grid.Row="3"
                   Text="{Binding ErrorMessage}"
                   Foreground="Red" TextWrapping="Wrap"
                   Margin="0,0,0,5"/>

        <!-- ── Row 4: Primary Data (star-sized) ── -->
        <DataGrid Grid.Row="4"
                  ItemsSource="{Binding Items}"
                  SelectedItem="{Binding SelectedItem}"
                  AutoGenerateColumns="False"
                  IsReadOnly="True"
                  SelectionMode="Single">
            <DataGrid.Columns>
                <DataGridTextColumn Header="ID" Binding="{Binding Id}" Width="60"/>
                <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="*"/>
                <DataGridTextColumn Header="Value" Binding="{Binding Value, StringFormat=C}" Width="120"/>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</UserControl>
```

### Why this works

- **Rows 0–3** use `Auto` and collapse to zero when empty (e.g., hidden form, no error text).
- **Row 4** uses `*` so the `DataGrid` expands into all remaining vertical space — no cropping, no dead whitespace.
- `Margin="20"` on the root `Grid` provides consistent page padding matching existing views.
- `DataGridTextColumn Width="*"` ensures at least one column stretches horizontally.

---

## Standard Dialog Layout (BaseDialogWindow)

Every dialog inherits `BaseDialogWindow`. Sizing is controlled by
`DialogWidth`/`DialogHeight` overrides and `WindowSizingService`.
**Never set `Width`, `Height`, `ResizeMode`, or `WindowStartupLocation` in XAML.**

```xml
<core:BaseDialogWindow
        x:Class="StoreAssistantPro.Modules.{Module}.Views.{Name}Window"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:core="clr-namespace:StoreAssistantPro.Core"
        Title="{Name}">
    <Grid Margin="25">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>   <!-- Dialog title -->
            <RowDefinition Height="*"/>      <!-- Scrollable body -->
            <RowDefinition Height="Auto"/>   <!-- Validation -->
            <RowDefinition Height="Auto"/>   <!-- Action buttons -->
        </Grid.RowDefinitions>

        <!-- ── Row 0: Title ── -->
        <TextBlock Grid.Row="0"
                   Text="Dialog Title"
                   FontSize="20" FontWeight="Bold"
                   Margin="0,0,0,15"/>

        <!-- ── Row 1: Body in ScrollViewer ── -->
        <ScrollViewer Grid.Row="1"
                      VerticalScrollBarVisibility="Auto"
                      HorizontalScrollBarVisibility="Disabled">
            <StackPanel>
                <StackPanel Margin="0,0,0,12">
                    <TextBlock Text="Field Label *"
                               FontSize="12" FontWeight="SemiBold"
                               Margin="0,0,0,3"/>
                    <TextBox Text="{Binding FieldValue,
                                 UpdateSourceTrigger=PropertyChanged}"
                             Padding="6" MaxLength="200"/>
                </StackPanel>

                <StackPanel Margin="0,0,0,12">
                    <TextBlock Text="Multiline Field"
                               FontSize="12" FontWeight="SemiBold"
                               Margin="0,0,0,3"/>
                    <TextBox Text="{Binding MultilineValue,
                                 UpdateSourceTrigger=PropertyChanged}"
                             Padding="6" MaxLength="500"
                             TextWrapping="Wrap" AcceptsReturn="True"
                             MinHeight="50"
                             VerticalScrollBarVisibility="Auto"/>
                </StackPanel>
            </StackPanel>
        </ScrollViewer>

        <!-- ── Row 2: Validation ── -->
        <StackPanel Grid.Row="2" Margin="0,0,0,5">
            <TextBlock Text="{Binding ErrorMessage}"
                       Foreground="Red" TextWrapping="Wrap"/>
            <TextBlock Text="{Binding SuccessMessage}"
                       Foreground="#4CAF50" TextWrapping="Wrap"/>
        </StackPanel>

        <!-- ── Row 3: Actions ── -->
        <StackPanel Grid.Row="3"
                    Orientation="Horizontal"
                    HorizontalAlignment="Right">
            <Button Content="💾 Save" Command="{Binding SaveCommand}"
                    Padding="16,8" FontSize="13" FontWeight="SemiBold"
                    Margin="0,0,8,0"/>
            <Button Content="Close" IsCancel="True"
                    Padding="16,8" FontSize="13"/>
        </StackPanel>
    </Grid>
</core:BaseDialogWindow>
```

### Why this works

- The `ScrollViewer` in Row 1 (`*`) ensures the form body scrolls when content exceeds the fixed dialog height — fields are never cropped.
- Action buttons are pinned to the bottom via the `Auto`-sized Row 3.
- `Margin="25"` matches the existing dialog padding convention.

---

## Grid Column Patterns

### Two-column form (label + input)

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto" MinWidth="80"/>  <!-- Labels -->
        <ColumnDefinition Width="*"/>                    <!-- Inputs stretch -->
    </Grid.ColumnDefinitions>

    <TextBlock Grid.Row="0" Grid.Column="0" Text="Name"
               VerticalAlignment="Center" Margin="0,0,10,8"/>
    <TextBox   Grid.Row="0" Grid.Column="1"
               Text="{Binding Name}" Padding="6" Margin="0,0,0,8"/>
</Grid>
```

### Multi-column data entry (equal weight)

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>    <!-- Column A -->
        <ColumnDefinition Width="*"/>    <!-- Column B -->
        <ColumnDefinition Width="Auto"/> <!-- Action buttons -->
    </Grid.ColumnDefinitions>
</Grid>
```

### Sidebar + Content (as in MainWindow / SystemSettingsWindow)

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="200" MinWidth="160"/>  <!-- Navigation sidebar -->
        <ColumnDefinition Width="*"/>                    <!-- Content area stretches -->
        <ColumnDefinition Width="Auto"/>                 <!-- Optional side panel -->
    </Grid.ColumnDefinitions>
</Grid>
```

---

## MinWidth / MinHeight Safety Net

Apply `MinWidth` and `MinHeight` to controls that must remain functional even when
their star-sized container is squeezed.

| Control | Recommended Minimum |
|---|---|
| `TextBox` (single-line input) | `MinWidth="80"` |
| `TextBox` (multiline) | `MinHeight="50"` |
| `DataGrid` | `MinHeight="100"` |
| `ComboBox` | `MinWidth="100"` |
| `Button` (action) | `MinWidth="60"` |
| Sidebar (`ColumnDefinition`) | `MinWidth="160"` |

### Example

```xml
<DataGrid Grid.Row="4"
          MinHeight="100"
          ItemsSource="{Binding Items}"
          AutoGenerateColumns="False"
          IsReadOnly="True"
          SelectionMode="Single">
    <DataGrid.Columns>
        <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="*"/>
        <DataGridTextColumn Header="Price" Binding="{Binding Price, StringFormat=C}"
                            Width="120" MinWidth="80"/>
    </DataGrid.Columns>
</DataGrid>
```

---

## Anti-Patterns to Avoid

### ❌ All-Auto rows with no star row

The content has no room to expand. Extra space pools at the bottom; insufficient
space clips the last rows.

```xml
<!-- BAD -->
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
</Grid.RowDefinitions>
```

**Fix:** Change the primary data row to `Height="*"`.

### ❌ Fixed pixel heights on data containers

```xml
<!-- BAD — clips content on high-DPI, wastes space on large screens -->
<DataGrid Height="400" ... />
```

**Fix:** Place the `DataGrid` in a star-sized row and remove the `Height`.

### ❌ Deeply nested StackPanels for page structure

`StackPanel` gives each child its desired size and does not constrain — children
can exceed the available viewport.

```xml
<!-- BAD — outer StackPanel never clips or scrolls -->
<StackPanel>
    <TextBlock Text="Header"/>
    <DataGrid ItemsSource="{Binding Items}"/>
    <StackPanel Orientation="Horizontal">...</StackPanel>
</StackPanel>
```

**Fix:** Use `Grid` with star rows for the outer structure. Reserve `StackPanel`
for small, local groupings (button rows, label+input pairs).

### ❌ Setting sizing properties on BaseDialogWindow subclasses in XAML

```xml
<!-- BAD — conflicts with WindowSizingService -->
<core:BaseDialogWindow Width="500" Height="400" ResizeMode="CanResize" ...>
```

**Fix:** Override `DialogWidth` / `DialogHeight` in code-behind only.

---

## Enterprise Page Layout — Full Example

Below is a complete, production-ready page that follows every rule above.
Use this as the starting point for any new module view.

```xml
<UserControl x:Class="StoreAssistantPro.Modules.Inventory.Views.InventoryView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:h="clr-namespace:StoreAssistantPro.Core.Helpers"
             mc:Ignorable="d"
             d:DesignHeight="550" d:DesignWidth="800"
             Loaded="OnLoaded">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>   <!-- Header -->
            <RowDefinition Height="Auto"/>   <!-- Search + toolbar -->
            <RowDefinition Height="Auto"/>   <!-- Inline form (collapsible) -->
            <RowDefinition Height="Auto"/>   <!-- Error / success messages -->
            <RowDefinition Height="*"/>      <!-- Data grid fills rest -->
            <RowDefinition Height="Auto"/>   <!-- Detail / summary strip -->
        </Grid.RowDefinitions>

        <!-- ═══════ Row 0: Page Header ═══════ -->
        <DockPanel Grid.Row="0" Margin="0,0,0,10">
            <TextBlock Text="Inventory" FontSize="24" FontWeight="Bold"
                       DockPanel.Dock="Left" VerticalAlignment="Center"/>
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <TextBlock VerticalAlignment="Center" Margin="0,0,8,0">
                    <Run Text="Items: " Foreground="#999"/>
                    <Run Text="{Binding TotalCount, Mode=OneWay}" FontWeight="SemiBold"/>
                </TextBlock>
                <Button Content="🔄 Refresh" Command="{Binding LoadCommand}"
                        Padding="12,6" FontSize="13"/>
            </StackPanel>
        </DockPanel>

        <!-- ═══════ Row 1: Search + Actions ═══════ -->
        <Grid Grid.Row="1" Margin="0,0,0,10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <!-- Search box with placeholder -->
            <Grid Grid.Column="0" Margin="0,0,10,0">
                <TextBox x:Name="SearchBox"
                         Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                         MinWidth="200" Padding="5"
                         VerticalContentAlignment="Center"/>
                <TextBlock Text="🔍 Search..."
                           IsHitTestVisible="False"
                           Foreground="#999" Padding="7,6"
                           VerticalAlignment="Center">
                    <TextBlock.Style>
                        <Style TargetType="TextBlock">
                            <Setter Property="Visibility" Value="Collapsed"/>
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding Text, ElementName=SearchBox}" Value="">
                                    <Setter Property="Visibility" Value="Visible"/>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </TextBlock.Style>
                </TextBlock>
            </Grid>

            <!-- Spacer (star column) keeps buttons right-aligned -->

            <StackPanel Grid.Column="2" Orientation="Horizontal">
                <Button Content="➕ Add" Command="{Binding ShowAddFormCommand}"
                        Padding="10,5" Margin="0,0,5,0"
                        Visibility="{Binding CanManage,
                            Converter={StaticResource BoolToVisibility}}"/>
                <Button Content="✏️ Edit" Command="{Binding ShowEditFormCommand}"
                        Padding="10,5" Margin="0,0,5,0"
                        Visibility="{Binding CanManage,
                            Converter={StaticResource BoolToVisibility}}"/>
                <Button Content="🗑 Delete" Command="{Binding DeleteCommand}"
                        Padding="10,5"
                        Visibility="{Binding CanDelete,
                            Converter={StaticResource BoolToVisibility}}"/>
            </StackPanel>
        </Grid>

        <!-- ═══════ Row 2: Inline Add/Edit Form (collapsible) ═══════ -->
        <Border Grid.Row="2" Background="#F5F5F5"
                CornerRadius="6" Padding="15" Margin="0,0,0,10"
                Visibility="{Binding IsFormVisible,
                    Converter={StaticResource BoolToVisibility}}">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto" MinWidth="80"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <StackPanel Grid.Column="0" Margin="0,0,15,0">
                    <TextBlock Text="Name" FontSize="12" Margin="0,0,0,3"/>
                    <TextBox Text="{Binding FormName, UpdateSourceTrigger=PropertyChanged}"
                             Padding="5" MinWidth="120"/>
                </StackPanel>

                <StackPanel Grid.Column="1" Margin="0,0,15,0">
                    <TextBlock Text="Price" FontSize="12" Margin="0,0,0,3"/>
                    <TextBox Text="{Binding FormPrice, UpdateSourceTrigger=PropertyChanged}"
                             h:NumericInput.IsDecimalOnly="True"
                             Padding="5" MinWidth="80"/>
                </StackPanel>

                <StackPanel Grid.Column="2" Margin="0,0,15,0">
                    <TextBlock Text="Qty" FontSize="12" Margin="0,0,0,3"/>
                    <TextBox Text="{Binding FormQuantity, UpdateSourceTrigger=PropertyChanged}"
                             h:NumericInput.IsIntegerOnly="True"
                             Padding="5" MinWidth="60"/>
                </StackPanel>

                <StackPanel Grid.Column="3" VerticalAlignment="Bottom"
                            Orientation="Horizontal">
                    <Button Content="💾 Save" Command="{Binding SaveCommand}"
                            Padding="10,5" Margin="0,0,5,0"/>
                    <Button Content="Cancel" Command="{Binding CancelCommand}"
                            Padding="10,5"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- ═══════ Row 3: Validation Messages ═══════ -->
        <StackPanel Grid.Row="3" Margin="0,0,0,5">
            <TextBlock Text="{Binding ErrorMessage}"
                       Foreground="Red" TextWrapping="Wrap"/>
            <TextBlock Text="{Binding SuccessMessage}"
                       Foreground="#4CAF50" TextWrapping="Wrap"/>
        </StackPanel>

        <!-- ═══════ Row 4: Primary Data Grid (star-sized) ═══════ -->
        <!-- Loading overlay -->
        <TextBlock Grid.Row="4" Text="⏳ Loading..." FontSize="14"
                   Foreground="#888"
                   HorizontalAlignment="Center" VerticalAlignment="Center"
                   Visibility="{Binding IsLoading,
                       Converter={StaticResource BoolToVisibility}}"/>

        <DataGrid Grid.Row="4"
                  MinHeight="100"
                  ItemsSource="{Binding Items}"
                  SelectedItem="{Binding SelectedItem}"
                  AutoGenerateColumns="False"
                  IsReadOnly="True"
                  SelectionMode="Single"
                  CanUserAddRows="False">
            <DataGrid.Columns>
                <DataGridTextColumn Header="ID"   Binding="{Binding Id}"   Width="60"/>
                <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="*"/>
                <DataGridTextColumn Header="Price"
                    Binding="{Binding Price, StringFormat=C}" Width="120" MinWidth="80"/>
                <DataGridTextColumn Header="Qty"
                    Binding="{Binding Quantity}" Width="80"/>
            </DataGrid.Columns>
        </DataGrid>

        <!-- ═══════ Row 5: Detail Strip (Auto, visible on selection) ═══════ -->
        <Border Grid.Row="5" Background="#F5F5F5"
                BorderBrush="#E0E0E0" BorderThickness="0,1,0,0"
                Padding="12,8"
                Visibility="{Binding HasSelection,
                    Converter={StaticResource BoolToVisibility}}">
            <StackPanel Orientation="Horizontal">
                <TextBlock FontWeight="SemiBold" FontSize="13">
                    <Run Text="Selected: "/>
                    <Run Text="{Binding SelectedItem.Name, Mode=OneWay}"/>
                </TextBlock>
                <TextBlock Foreground="#666" Margin="15,0,0,0">
                    <Run Text="Stock: "/>
                    <Run Text="{Binding SelectedItem.Quantity, Mode=OneWay}"/>
                </TextBlock>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

---

## Quick Checklist for Every New View

- [ ] Root container is `Grid` (not `StackPanel` or `DockPanel`)
- [ ] Exactly **one** row uses `Height="*"` for the primary data area
- [ ] All other rows use `Height="Auto"`
- [ ] `DataGrid` lives in the star-sized row with no fixed `Height`
- [ ] At least one `DataGridTextColumn` uses `Width="*"`
- [ ] `TextBlock` displaying user/dynamic content has `TextWrapping="Wrap"`
- [ ] Inline forms use `Grid` with star/auto columns, not deeply nested `StackPanel`
- [ ] `MinWidth`/`MinHeight` set on inputs and data controls
- [ ] Dialogs inherit `BaseDialogWindow` — no sizing attributes in XAML
- [ ] `Margin="20"` on root Grid for pages, `Margin="25"` for dialogs
