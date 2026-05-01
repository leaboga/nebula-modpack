with open('Themes/Styles.xaml', 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('<Border x:Name="bg" Background="Transparent" CornerRadius="10" Padding="14,10" Margin="0,2">', 
                    '<Border x:Name="bg" Background="Transparent" CornerRadius="10" Padding="14,10" Margin="0,2" TextElement.Foreground="{TemplateBinding Foreground}" FontWeight="SemiBold">')

with open('Themes/Styles.xaml', 'w', encoding='utf-8') as f:
    f.write(text)
