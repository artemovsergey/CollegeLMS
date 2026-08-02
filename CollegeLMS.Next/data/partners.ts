export interface Partner {
  name: string
  description: string
  href: string
  logo: string
}

export const partners: Partner[] = [
  {
    name: "АО «ОДК»",
    description: "Объединённая двигателестроительная корпорация, партнёр в подготовке кадров",
    href: "https://www.uecrus.com/",
    logo: "/images/partners/odk.svg",
  },
  {
    name: "Министерство промышленности Ставропольского края",
    description: "Учредитель колледжа, курирующий образовательную деятельность",
    href: "https://stavminprom.ru/",
    logo: "/images/partners/minprom.png",
  },
  {
    name: "ПАО «Ростелеком»",
    description: "Крупнейший провайдер цифровых услуг, база практики студентов",
    href: "https://company.rt.ru/",
    logo: "/images/partners/rostelecom.svg",
  },
  {
    name: "АО «ЭР-Телеком»",
    description: "Телекоммуникационная компания, партнёр в сфере IT и связи",
    href: "https://ertelecom.ru/",
    logo: "/images/partners/er-telecom.svg",
  },
]
