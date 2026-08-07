import json

with open('public/i18n/en/profile.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

data['PROFILE']['SELECT_POSITIONS_INSTRUCTION'] = 'Select up to 5 positions. Click \"{{setPrimary}}\" on any selected tag below to make it your main position.'

with open('public/i18n/en/profile.json', 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=4)

with open('public/i18n/ar/profile.json', 'r', encoding='utf-8') as f:
    data_ar = json.load(f)

data_ar['PROFILE']['SELECT_POSITIONS_INSTRUCTION'] = 'اختر ما يصل إلى 5 مراكز. اضغط على \"{{setPrimary}}\" على أي من العلامات المحددة أدناه لتعيينه كمركزك الأساسي.'

with open('public/i18n/ar/profile.json', 'w', encoding='utf-8') as f:
    json.dump(data_ar, f, indent=4, ensure_ascii=False)
