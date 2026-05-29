import re

with open('Modules/ConfigView.xaml', 'r', encoding='utf-8') as f:
    content = f.read()

# We need to remove the whole Appearance Card or simplify it.
# The card starts with: <!-- Appearance Card -->
# And ends with: </Border> before <!-- Presets Section -->

pattern = re.compile(r'<!-- Appearance Card -->.*?</Border>\s*<!-- Presets Section -->', re.DOTALL)

# Let's replace the whole Appearance Card with just a simpler version that only has Background Wallpaper (FONDO DE PANTALLA).

new_appearance_card = """<!-- Appearance Card -->
            <Border Background="#0A0E14" CornerRadius="16" Padding="24,20" Margin="0,0,0,16" BorderBrush="#0D151D" BorderThickness="1">
                <StackPanel>
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,16">
                        <Border Width="32" Height="32" CornerRadius="9" Background="#EC4899">
                            <TextBlock Text="🎨" FontSize="16" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <TextBlock Text="Fondo de Pantalla" Foreground="#E6F9FF" FontSize="15" FontWeight="SemiBold" Margin="12,0,0,0" VerticalAlignment="Center"/>
                    </StackPanel>

                    <TextBlock Text="IMAGEN PERSONALIZADA" Foreground="#344A5B" FontSize="9" FontWeight="Bold" Margin="2,0,0,12"/>
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        <TextBox x:Name="BgPathBox" IsReadOnly="True" Style="{DynamicResource AppleTextBox}"/>
                        <StackPanel Grid.Column="1" Orientation="Horizontal" Margin="10,0,0,0">
                            <Button Content="..." Width="34" Height="38" Style="{DynamicResource SecondaryButton}" Click="BtnBrowseBackground_Click"/>
                            <Button Content="🗑️" Width="34" Height="38" Style="{DynamicResource SecondaryButton}" Margin="4,0,0,0" Click="BtnResetBackground_Click"/>
                        </StackPanel>
                    </Grid>
                    <Border Margin="0,12,0,0" Height="80" CornerRadius="12" BorderBrush="#0D151D" BorderThickness="1" Background="#05080D" ClipToBounds="True">
                        <Grid>
                            <Image x:Name="BgPreviewImage" Stretch="UniformToFill" Opacity="0.5">
                                <Image.Effect>
                                    <BlurEffect Radius="2"/>
                                </Image.Effect>
                            </Image>
                            <TextBlock Text="PREVISUALIZACIÓN" Foreground="#2A2440" FontSize="9" FontWeight="Bold" HorizontalAlignment="Center" VerticalAlignment="Center" x:Name="PreviewPlaceholderText"/>
                        </Grid>
                    </Border>
                </StackPanel>
            </Border>

            <!-- Presets Section -->"""

content = pattern.sub(new_appearance_card, content)

with open('Modules/ConfigView.xaml', 'w', encoding='utf-8') as f:
    f.write(content)

print('Appearance card simplified.')
