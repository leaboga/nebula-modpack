with open('Themes/Styles.xaml', 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace(' FontWeight="SemiBold">', ' TextElement.FontWeight="SemiBold">')

with open('Themes/Styles.xaml', 'w', encoding='utf-8') as f:
    f.write(text)
