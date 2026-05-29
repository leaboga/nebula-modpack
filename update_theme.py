# -*- coding: utf-8 -*-
import io
with open('Themes/Styles.xaml', 'r', encoding='utf-8') as f:
    styles = f.read()

# Palette changes
styles = styles.replace('<Color x:Key="AccentColor">#00F2FF</Color>', '<Color x:Key="AccentColor">#0A84FF</Color>')
styles = styles.replace('<Color x:Key="AccentHoverColor">#70F9FF</Color>', '<Color x:Key="AccentHoverColor">#409CFF</Color>')
styles = styles.replace('<Color x:Key="AccentPressColor">#00B8C2</Color>', '<Color x:Key="AccentPressColor">#0060DF</Color>')

styles = styles.replace('<Color x:Key="SurfaceColor">#060A0F</Color>', '<Color x:Key="SurfaceColor">#000000</Color>')
styles = styles.replace('<Color x:Key="Surface2Color">#101821</Color>', '<Color x:Key="Surface2Color">#1C1C1E</Color>')
styles = styles.replace('<Color x:Key="Surface3Color">#172330</Color>', '<Color x:Key="Surface3Color">#2C2C2E</Color>')
styles = styles.replace('<Color x:Key="BorderColor">#253544</Color>', '<Color x:Key="BorderColor">#38383A</Color>')
styles = styles.replace('<Color x:Key="GlowColor">#00F2FF</Color>', '<Color x:Key="GlowColor">#0A84FF</Color>')

# Glass panel to be cleaner
styles = styles.replace('<Setter Property="Background" Value="#C00C1218"/>', '<Setter Property="Background" Value="#881C1C1E"/>')
styles = styles.replace('<Setter Property="BorderBrush" Value="#1E2C38"/>', '<Setter Property="BorderBrush" Value="#38383A"/>')

# ToggleTab checked background
styles = styles.replace('<Setter TargetName="bg" Property="Background" Value="#141E29"/>', '<Setter TargetName="bg" Property="Background" Value="#2C2C2E"/>')
# ToggleTab default foreground
styles = styles.replace('<Setter Property="Foreground" Value="#6D8A99"/>', '<Setter Property="Foreground" Value="#8E8E93"/>')

with open('Themes/Styles.xaml', 'w', encoding='utf-8') as f:
    f.write(styles)

# Now MainWindow.xaml background colors
with open('MainWindow.xaml', 'r', encoding='utf-8') as f:
    main = f.read()

main = main.replace('<GradientStop Color="#020507" Offset="0"/>', '<GradientStop Color="#000000" Offset="0"/>')
main = main.replace('<GradientStop Color="#07151A" Offset="0.48"/>', '<GradientStop Color="#1C1C1E" Offset="0.48"/>')
main = main.replace('<GradientStop Color="#020405" Offset="1"/>', '<GradientStop Color="#000000" Offset="1"/>')
main = main.replace('<GradientStop Color="#081016" Offset="0"/>', '<GradientStop Color="#000000" Offset="0"/>')
main = main.replace('<GradientStop Color="#05080D" Offset="1"/>', '<GradientStop Color="#1C1C1E" Offset="1"/>')
main = main.replace('Fill="#2618E0D2"', 'Fill="#150A84FF"')
main = main.replace('Fill="#14316063"', 'Fill="#100A84FF"')
main = main.replace('Background="#0A0E14"', 'Background="#1C1C1E"')
main = main.replace('BorderBrush="#0D151D"', 'BorderBrush="#38383A"')
main = main.replace('BorderBrush="#1E2C38"', 'BorderBrush="#38383A"')
main = main.replace('Background="#0D151D"', 'Background="#1C1C1E"')
main = main.replace('BorderBrush="#00F2FF"', 'BorderBrush="#0A84FF"')
main = main.replace('Foreground="#00F2FF"', 'Foreground="#0A84FF"')

with open('MainWindow.xaml', 'w', encoding='utf-8') as f:
    f.write(main)

print("Themes updated")
